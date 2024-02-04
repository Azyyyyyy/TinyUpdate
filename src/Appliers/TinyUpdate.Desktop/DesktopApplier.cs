using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using TinyUpdate.Core.Abstract.Delta;
using TinyUpdate.Core.Abstract.UpdatePackage;
using TinyUpdate.Desktop.Abstract;

namespace TinyUpdate.Desktop;

public class DesktopApplier : IUpdateApplier
{
    private readonly IHasher _hasher;
    private readonly ILogger<DesktopApplier> _logger;
    private readonly IDeltaManager _deltaManager;
    private readonly INative? _native;
    private readonly IFileSystem _fileSystem;
    public DesktopApplier(ILogger<DesktopApplier> logger, IHasher hasher, INative? native, IDeltaManager deltaManager, IFileSystem fileSystem)
    {
        _logger = logger;
        _hasher = hasher;
        _native = native;
        _deltaManager = deltaManager;
        _fileSystem = fileSystem;
    }
    
    public bool SupportedOS() => OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

    public async Task<bool> ApplyUpdates(ICollection<IUpdatePackage> updatePackages, string applicationLocation,
        IProgress<double>? progress = null)
    {
        var previousVersion = updatePackages.OrderBy(x => x.ReleaseEntry.PreviousVersion).First().ReleaseEntry.PreviousVersion;
        var newVersion = updatePackages.OrderBy(x => x.ReleaseEntry.PreviousVersion).Last().ReleaseEntry.PreviousVersion;

        var previousVersionLocation = Path.Combine(applicationLocation, previousVersion.ToString());
        var newVersionLocation = Path.Combine(applicationLocation, newVersion.ToString());
        var multiProgress = progress != null
            ? new MultiProgress(progress, updatePackages.Count)
            : null;

        foreach (var updatePackage in updatePackages)
        {
            var successful =
                await ApplyUpdate(updatePackage, previousVersionLocation, newVersionLocation, multiProgress);
            if (!successful)
            {
                return false;
            }
            multiProgress?.Bump();
        }

        return true;
    }

    public Task<bool> Cleanup()
    {
        //TODO: Imp
        return Task.FromResult(true);
    }
    
    public Task<bool> ApplyUpdate(IUpdatePackage updatePackage, string applicationLocation, IProgress<double>? progress = null)
    {
        var newVersionLocation = Path.Combine(applicationLocation, updatePackage.ReleaseEntry.NewVersion.ToString());
        string? previousVersionLocation = null;
        if (updatePackage.ReleaseEntry.IsDelta)
        {
            previousVersionLocation = Path.Combine(applicationLocation, updatePackage.ReleaseEntry.PreviousVersion.ToString());
        }

        return ApplyUpdate(updatePackage, previousVersionLocation, newVersionLocation, progress);
    }
    
    private async Task<bool> ApplyUpdate(IUpdatePackage updatePackage, string? previousVersionLocation, 
        string newVersionLocation, IProgress<double>? progress)
    {
        double progressTotal = 0;
        var filePercent = (double)1 / updatePackage.FileCount;

        if (updatePackage.ReleaseEntry.IsDelta
            && string.IsNullOrWhiteSpace(previousVersionLocation))
        {
            _logger.NoPreviousVersion();
            return false;
        }

        //Create root and sub-directories beforehand
        _fileSystem.Directory.CreateDirectory(newVersionLocation);
        if (updatePackage.Directories.Count > 0)
        {
            foreach (var directory in updatePackage.Directories)
            {
                _fileSystem.Directory.CreateDirectory(Path.Combine(newVersionLocation, directory));
            }
        }

        if (updatePackage.NewFiles.Count > 0)
        {
            foreach (var newFile in updatePackage.NewFiles)
            {
                var newPath = Path.Combine(newVersionLocation, newFile.Location);
                await using var newFileStream = _fileSystem.File.Open(newPath, new FileStreamOptions
                {
                    PreallocationSize = newFile.Filesize, 
                    Mode = FileMode.Create
                });

                await newFile.Stream.CopyToAsync(newFileStream);
                await newFile.Stream.DisposeAsync();

                newFileStream.Seek(0, SeekOrigin.Begin);
                if (!CheckFile(newFileStream, newFile.Hash, newFile.Filesize, newPath))
                {
                    return false;
                }
                UpdateProgress();
            }
        }

        //If this isn't a delta update then we'll only have new files
        if (!updatePackage.ReleaseEntry.IsDelta)
        {
            Debug.Assert(updatePackage.FileCount != updatePackage.NewFiles.Count, "Non delta update isn't all new files");
            return true;
        }

        if (updatePackage.MovedFiles.Count > 0)
        {
            foreach (var movedFile in updatePackage.MovedFiles)
            {
                if (string.IsNullOrWhiteSpace(movedFile.PreviousLocation))
                {
                    _logger.NoPreviousLocation(movedFile.Location);
                    return false;
                }

                if (!ProcessHardLinkableFile(movedFile.PreviousLocation, movedFile.Location, movedFile.Hash, 
                        movedFile.Filesize))
                {
                    return false;
                }
                UpdateProgress();
            }
        }

        if (updatePackage.UnchangedFiles.Count > 0)
        {
            foreach (var unchangedFile in updatePackage.UnchangedFiles)
            {
                if (!ProcessHardLinkableFile(unchangedFile.Location, unchangedFile.Location, 
                        unchangedFile.Hash, unchangedFile.Filesize))
                {
                    return false;
                }
                UpdateProgress();
            }
        }

        if (updatePackage.DeltaFiles.Count > 0)
        {
            foreach (var deltaFile in updatePackage.DeltaFiles)
            {
                Debug.Assert(previousVersionLocation != null, nameof(previousVersionLocation) + " != null");
                var sourcePath = Path.Combine(previousVersionLocation, deltaFile.Location);
                var targetPath = Path.Combine(newVersionLocation, deltaFile.Location);

                await using var sourceStream = _fileSystem.File.OpenRead(sourcePath);
                await using var targetStream = _fileSystem.File.Open(targetPath, new FileStreamOptions
                {
                    PreallocationSize = deltaFile.Filesize,
                    Mode = FileMode.Create, 
                    Access = FileAccess.ReadWrite,
                    Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                    Share = FileShare.None
                });

                var successful = await _deltaManager.ApplyDeltaUpdate(deltaFile, sourceStream, targetStream);
                await deltaFile.Stream.DisposeAsync();

                if (!successful)
                {
                    _logger.FailedDeltaApply(targetPath);
                    return false;
                }
            
                targetStream.Seek(0, SeekOrigin.Begin);
                if (!CheckFile(targetStream, deltaFile.Hash, deltaFile.Filesize, targetPath))
                {
                    return false;
                }
                UpdateProgress();
            }
        }

        return true;

        /*-----------------*/
        /* Local functions */
        /*-----------------*/
        void UpdateProgress()
        {
            progressTotal += filePercent;
            progress?.Report(progressTotal);
        }
        
        bool ProcessHardLinkableFile(string previousRelativePath, string newRelativePath, string expectedHash, long expectedFilesize)
        {
            var previousPath = Path.Combine(previousVersionLocation, previousRelativePath);
            var newPath = Path.Combine(newVersionLocation, newRelativePath);
            Stream fileStream;

            //Attempt to create the file as a hard link
            if (_native?.CreateHardLink(previousPath, newPath) ?? false)
            {
                using (fileStream = _fileSystem.File.OpenRead(newPath))
                {
                    return CheckFile(fileStream, expectedHash, expectedFilesize, newPath);
                }
            }
            
            //We failed, we'll copy the file as a fallback method
            _logger.HardLinkFailed(previousPath, newPath);
            _fileSystem.File.Copy(previousPath, newPath, true);
            using (fileStream = _fileSystem.File.OpenRead(newPath))
            {
                return CheckFile(fileStream, expectedHash, expectedFilesize, newPath);
            }
        }
    }

    private bool CheckFile(Stream targetStream, string expectedHash, long expectedFilesize, string filePath)
    {
        if (targetStream.Length != expectedFilesize)
        {
            _logger.FilesizeMisMatch(filePath);
            return false;
        }
        
        if (!_hasher.CompareHash(targetStream, expectedHash))
        {
            _logger.FileHashMisMatch(filePath);
            return false;
        }

        return true;
    }
}