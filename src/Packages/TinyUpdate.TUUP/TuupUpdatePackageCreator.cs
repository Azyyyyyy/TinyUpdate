using System.IO.Abstractions;
using System.IO.Compression;
using NeoSmart.AsyncLock;
using SemVersion;
using TinyUpdate.Core.Abstract;

namespace TinyUpdate.TUUP;

public class TuupUpdatePackageCreator : IUpdatePackageCreator
{
    private readonly AsyncLock _zipLock;
    private readonly IHasher _hasher;
    private readonly IDeltaManager _deltaManager;
    private readonly TuupUpdatePackageCreatorOptions _options;
    
    // ReSharper disable InconsistentNaming
    private readonly IDirectory Directory;
    private readonly IFile File;
    // ReSharper restore InconsistentNaming

    // ReSharper disable once ConvertToPrimaryConstructor
    public TuupUpdatePackageCreator(IHasher hasher, IDeltaManager deltaManager, IFileSystem fileSystem, TuupUpdatePackageCreatorOptions options)
    {
        _hasher = hasher;
        _deltaManager = deltaManager;
        _options = options;
        Directory = fileSystem.Directory;
        File = fileSystem.File;
        _zipLock = new AsyncLock();
    }

    public string Extension => Consts.TuupExtension;
    public string FullPackageFilenameTemplate => "{0}-{1}-full" + Extension;
    public string DeltaPackageFilenameTemplate => "{0}-{1}-delta" + Extension;

    public async Task<bool> CreateFullPackage(string applicationLocation, SemanticVersion applicationVersion, string updatePackageLocation,
        string applicationName, IProgress<double>? progress = null)
    {
        var files = Directory.GetFiles(applicationLocation, "*", SearchOption.AllDirectories);
        using var zipArchive =
            CreateZipArchive(Path.Combine(updatePackageLocation, string.Format(FullPackageFilenameTemplate, applicationName, applicationVersion)));

        for (int i = 0; i < files.Length; i++)
        {
            var fileLocation = files[i];
            await using var fileContentStream = File.OpenRead(fileLocation);

            await AddNewFile(zipArchive, fileContentStream, Path.GetRelativePath(applicationLocation, fileLocation));
            progress?.Report((double)i / files.Length);
        }

        return true;
    }

    public async Task<bool> CreateDeltaPackage(string oldApplicationLocation, SemanticVersion oldApplicationVersion,
        string newApplicationLocation, SemanticVersion newApplicationVersion, string updatePackageLocation,
        string applicationName, IProgress<double>? progress = null)
    {
        var oldFiles = Directory.GetFiles(oldApplicationLocation, "*", SearchOption.AllDirectories);
        var newFiles = Directory.GetFiles(newApplicationLocation, "*", SearchOption.AllDirectories);
        Dictionary<string, List<string>> oldFilesHashes = new();
        Dictionary<string, List<string>> newFilesHashes = new();

        await GetHashes(oldFilesHashes, oldFiles);
        await GetHashes(newFilesHashes, newFiles);
        
        using var zipArchive =
            CreateZipArchive(Path.Combine(updatePackageLocation, string.Format(DeltaPackageFilenameTemplate, applicationName, newApplicationVersion)));

        for (int i = 0; i < newFiles.Length; ReportAndBump(ref i))
        {
            var newFile = newFiles[i];
            var newHash = newFilesHashes.First(x => x.Value.Any(y => y == newFile)).Key;
            var relativeNewFile = Path.GetRelativePath(newApplicationLocation, newFile);

            var oldFile = oldFiles.FirstOrDefault(x => Path.GetRelativePath(oldApplicationLocation, x) == relativeNewFile);
            await using var newFileContentStream = File.OpenRead(newFile);

            if (string.IsNullOrWhiteSpace(oldFile))
            {
                if (!await FindAndAddMovedFile(newHash, relativeNewFile, newFileContentStream.Length))
                {
                    await AddNewFile(zipArchive, newFileContentStream, relativeNewFile);
                }
                continue;
            }
            
            //See if the file is the same by the outputted hash
            var oldHash = oldFilesHashes.First(x => x.Value.Any(y => y == oldFile)).Key;
            if (newHash == oldHash)
            {
                await AddSameFile(zipArchive, relativeNewFile, newHash);
                continue;
            }

            await using var oldFileContentStream = File.OpenRead(oldFile);
            newFileContentStream.Seek(0, SeekOrigin.Begin);
            
            var deltaResult = await _deltaManager.CreateDeltaFile(oldFileContentStream, newFileContentStream);
            if (deltaResult.Successful)
            {
                deltaResult.DeltaStream.Seek(0, SeekOrigin.Begin);
                
                await AddFile(
                    zipArchive,
                    deltaResult.DeltaStream,
                    relativeNewFile + deltaResult.Creator.Extension);
            }
        }
        
        return true;

        async Task<bool> FindAndAddMovedFile(string newHash, string relativeNewFile, long filesize)
        {
            //V1 Tuup format didn't have the concept of moved files, if we want to make v1 packages then don't handle this
            if (!_options.V1Compatible && oldFilesHashes.TryGetValue(newHash, out var oldFilesList) && oldFilesList.Count > 0)
            {
                var oldFile = oldFilesList[0];
                var filepath = relativeNewFile + Consts.MovedFileExtension;
                using (await _zipLock.LockAsync())
                {
                    CheckFilePath(ref filepath);
                    
                    await using var zipShasumEntryStream = zipArchive.CreateEntry(filepath, CompressionLevel.SmallestSize).Open();
                    await using var shasumStreamWriter = new StreamWriter(zipShasumEntryStream);
                    var path = Path.GetRelativePath(oldApplicationLocation, oldFile);

                    CheckFilePath(ref path);
                    await shasumStreamWriter.WriteAsync(path);
                }

                await AddHashAndSizeData(zipArchive, filepath, newHash, filesize);

                return true;
            }
            
            return false;
        }
        
        async Task GetHashes(IDictionary<string, List<string>> hashes, IEnumerable<string> files)
        {
            //We want to get a hash of all the old files, this allows us to detect files which have moved
            foreach (var oldFile in files)
            {
                await using var oldFileContentStream = File.OpenRead(oldFile);
                var hash = _hasher.CreateHash(oldFileContentStream);
                if (!hashes.TryGetValue(hash, out var filesList))
                {
                    filesList = new List<string>();
                    hashes.Add(hash, filesList);
                }
            
                filesList.Add(oldFile);
            }
        }

        void ReportAndBump(ref int value)
        {
            progress?.Report((double)value / newFiles.Length); 
            value++;
        }
    }
    
    private ZipArchive CreateZipArchive(string updatePackageLocation)
    {
        var updatePackageStream = File.Create(updatePackageLocation);
        return new ZipArchive(updatePackageStream, ZipArchiveMode.Create);
    }

    private Task<bool> AddNewFile(ZipArchive zipArchive, Stream fileContentStream, string filepath) =>
        AddFile(zipArchive, fileContentStream, filepath + Consts.NewFileExtension);

    private Task<bool> AddSameFile(ZipArchive zipArchive, string filepath, string hash) =>
        AddFile(zipArchive, Stream.Null, filepath + Consts.UnchangedFileExtension, hash: hash);
    
    private async Task<bool> AddFile(ZipArchive zipArchive, Stream fileContentStream, string filepath, string? hash = null)
    {
        CheckFilePath(ref filepath);
        
        //Add the file
        using (await _zipLock.LockAsync())
        {
            var zipFileEntryStream = zipArchive.CreateEntry(filepath, CompressionLevel.SmallestSize).Open();
            await fileContentStream.CopyToAsync(zipFileEntryStream);
            await zipFileEntryStream.DisposeAsync();
        }
        
        hash ??= _hasher.CreateHash(fileContentStream);
        await AddHashAndSizeData(zipArchive, filepath, hash, fileContentStream.Length);

        return true;
    }

    private void CheckFilePath(ref string filepath)
    {
        if (Path.DirectorySeparatorChar != '\\')
        {
            filepath = filepath.Replace(Path.DirectorySeparatorChar, '\\');
        }
    }

    private async Task AddHashAndSizeData(ZipArchive zipArchive, string filepath, string hash, long filesize)
    {
        CheckFilePath(ref filepath);

        using (await _zipLock.LockAsync())
        {
            await using var zipShasumEntryStream = zipArchive.CreateEntry(Path.ChangeExtension(filepath, Consts.ShasumFileExtension), CompressionLevel.SmallestSize).Open();
            await using var shasumStreamWriter = new StreamWriter(zipShasumEntryStream);
            await shasumStreamWriter.WriteAsync($"{hash} {filesize}");
        }
    }
}