using Microsoft.Extensions.Logging;
using TinyUpdate.Core.Abstract.Delta;
using TinyUpdate.Desktop.Abstract;
using TinyUpdate.Desktop.Native;

namespace TinyUpdate.Desktop;

public class DesktopApplier : IUpdateApplier
{
    private readonly IHasher _hasher;
    private readonly ILogger _logger;
    private readonly IDeltaManager _deltaManager;
    private readonly INative? _native;
    public DesktopApplier(ILogger logger, IHasher hasher, INative? native, IDeltaManager deltaManager)
    {
        _logger = logger;
        _hasher = hasher;
        _native = native;
        _deltaManager = deltaManager;
    }
    
    public bool SupportedOS() => OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

    public async Task<bool> ApplyUpdates(ICollection<IUpdatePackage> updatePackages, string applicationLocation,
        IProgress<double>? progress = null)
    {
        var previousVersion = updatePackages.OrderBy(x => x.PreviousVersion).First().PreviousVersion;
        var newVersion = updatePackages.OrderBy(x => x.PreviousVersion).Last().PreviousVersion;

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
        var previousVersionLocation = Path.Combine(applicationLocation, updatePackage.PreviousVersion.ToString());
        var newVersionLocation = Path.Combine(applicationLocation, updatePackage.NewVersion.ToString());

        return ApplyUpdate(updatePackage, previousVersionLocation, newVersionLocation, progress);
    }   
    
    private async Task<bool> ApplyUpdate(IUpdatePackage updatePackage, string previousVersionLocation, 
        string newVersionLocation, IProgress<double>? progress)
    {
        double progressTotal = 0;
        var ind = (double)1 / updatePackage.FileCount;
        

        //Create all the directories beforehand
        foreach (var directory in updatePackage.Directories)
        {
            Directory.CreateDirectory(Path.Combine(newVersionLocation, directory));
        }
        
        foreach (var movedFile in updatePackage.MovedFiles)
        {
            if (string.IsNullOrWhiteSpace(movedFile.PreviousLocation))
            {
                _logger.LogError("{NewLocation} has no link to it's previous location", movedFile.Location);
                return false;
            }

            if (!ProcessHardLinkableFile(movedFile.PreviousLocation, movedFile.Location, movedFile.Hash, 
                    movedFile.Filesize))
            {
                return false;
            }
            UpdateProgress();
        }

        foreach (var unchangedFile in updatePackage.UnchangedFiles)
        {
            if (!ProcessHardLinkableFile(unchangedFile.Location, unchangedFile.Location, 
                    unchangedFile.Hash, unchangedFile.Filesize))
            {
                return false;
            }
            UpdateProgress();
        }

        foreach (var newFile in updatePackage.NewFiles)
        {
            var newPath = Path.Combine(newVersionLocation, newFile.Location);
            await using var newFileStream = File.Open(newPath, new FileStreamOptions
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

        foreach (var deltaFile in updatePackage.DeltaFiles)
        {
            var sourcePath = Path.Combine(previousVersionLocation, deltaFile.Location);
            var targetPath = Path.Combine(newVersionLocation, deltaFile.Location);

            await using var sourceStream = File.OpenRead(sourcePath);
            await using var targetStream = File.Open(targetPath, new FileStreamOptions
            {
                PreallocationSize = deltaFile.Filesize,
                Mode = FileMode.Create
            });

            var successful = await _deltaManager.ApplyDeltaUpdate(deltaFile, sourceStream, targetStream);
            await deltaFile.Stream.DisposeAsync();

            if (!successful)
            {
                _logger.LogError("Failed to apply the delta file for {NewPath}", targetPath);
                return false;
            }
            
            targetStream.Seek(0, SeekOrigin.Begin);
            if (!CheckFile(targetStream, deltaFile.Hash, deltaFile.Filesize, targetPath))
            {
                return false;
            }
            UpdateProgress();
        }

        return true;

        void UpdateProgress()
        {
            progressTotal += ind;
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
                using (fileStream = File.OpenRead(newPath))
                {
                    return CheckFile(fileStream, expectedHash, expectedFilesize, newPath);
                }
            }
            
            //We failed, we'll copy the file as a backup
            _logger.LogWarning("Was unable to hard link {PreviousPath} to {NewPath}, going to copy file", previousPath, newPath);
            File.Copy(previousPath, newPath, true);
            using (fileStream = File.OpenRead(newPath))
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