using System.IO.Abstractions;
using System.IO.Compression;
using NeoSmart.AsyncLock;
using SemVersion;
using TinyUpdate.Core.Abstract;
using TinyUpdate.Core.Abstract.Delta;

namespace TinyUpdate.TUUP;

/// <summary>
/// Update package creator for <see cref="TuupUpdatePackage"/>
/// </summary>
public class TuupUpdatePackageCreator : IDeltaUpdatePackageCreator, IUpdatePackageCreator
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

    public async Task<bool> CreateDeltaPackage(string previousApplicationLocation, SemanticVersion previousApplicationVersion,
        string newApplicationLocation, SemanticVersion newApplicationVersion, string updatePackageLocation,
        string applicationName, IProgress<double>? progress = null)
    {
        var previousFiles = Directory.GetFiles(previousApplicationLocation, "*", SearchOption.AllDirectories);
        var newFiles = Directory.GetFiles(newApplicationLocation, "*", SearchOption.AllDirectories);
        Dictionary<string, List<string>> previousFilesHashes = new();
        Dictionary<string, List<string>> newFilesHashes = new();

        //We make use of the hashes so we can find files that have moved!
        await GetHashes(previousFilesHashes, previousFiles);
        await GetHashes(newFilesHashes, newFiles);
        
        using var zipArchive =
            CreateZipArchive(Path.Combine(updatePackageLocation, string.Format(DeltaPackageFilenameTemplate, applicationName, newApplicationVersion)));

        for (int i = 0; i < newFiles.Length; ReportAndBump(ref i))
        {
            var newFilePath = newFiles[i];
            var newHash = newFilesHashes.First(x => x.Value.Any(y => y == newFilePath)).Key;
            var newFileRelativePath = Path.GetRelativePath(newApplicationLocation, newFilePath);

            //First see if the file is in the same place but changed
            var previousFilePath = previousFiles.FirstOrDefault(x => Path.GetRelativePath(previousApplicationLocation, x) == newFileRelativePath);
            await using var newFileContentStream = File.OpenRead(newFilePath);

            //If we can't find the path in it's original location, maybe it's moved
            if (string.IsNullOrWhiteSpace(previousFilePath))
            {
                if (!await FindAndAddMovedFile(newHash, newFileRelativePath, newFileContentStream.Length))
                {
                    // (It hasn't moved)
                    await AddNewFile(zipArchive, newFileContentStream, newFileRelativePath);
                }
                continue;
            }
            
            //See if the file is the same by the outputted hash
            var previousHash = previousFilesHashes.First(x => x.Value.Any(y => y == previousFilePath)).Key;
            if (newHash == previousHash)
            {
                await AddSameFile(zipArchive, newFileRelativePath, newHash);
                continue;
            }

            //Now the fun stuff, the contents has changed so we want to get a delta file :D
            await using var previousFileContentStream = File.OpenRead(previousFilePath);
            newFileContentStream.Seek(0, SeekOrigin.Begin);
            
            var deltaResult = await _deltaManager.CreateDeltaUpdate(previousFileContentStream, newFileContentStream);
            if (deltaResult.Successful)
            {
                deltaResult.DeltaStream.Seek(0, SeekOrigin.Begin);
                
                await AddFile(
                    zipArchive,
                    deltaResult.DeltaStream,
                    newFileRelativePath + deltaResult.Creator.Extension);
            }
        }
        
        return true;

        async Task<bool> FindAndAddMovedFile(string newHash, string relativeNewFile, long filesize)
        {
            //V1 Tuup format didn't have the concept of moved files, if we want to make v1 packages then don't handle this
            if (!_options.V1Compatible && previousFilesHashes.TryGetValue(newHash, out var previousFilesList) && previousFilesList.Count > 0)
            {
                var previousFileLocation = previousFilesList[0];
                var newFilePath = relativeNewFile + Consts.MovedFileExtension;
                using (await _zipLock.LockAsync())
                {
                    CheckFilePath(ref newFilePath);
                    await AddMovedPathFile(newFilePath, previousFileLocation);
                }

                await AddHashAndSizeData(zipArchive, newFilePath, newHash, filesize);
                return true;
            }
            
            return false;
        }

        async Task AddMovedPathFile(string newFilePath, string previousFileLocation)
        {
            await using var movedPathStream = zipArchive.CreateEntry(newFilePath, CompressionLevel.SmallestSize).Open();
            await using var movedPathStreamWriter = new StreamWriter(movedPathStream);
            var previousRelativePath = Path.GetRelativePath(previousApplicationLocation, previousFileLocation);

            CheckFilePath(ref previousRelativePath);
            await movedPathStreamWriter.WriteAsync(previousRelativePath);
        }
        
        async Task GetHashes(IDictionary<string, List<string>> hashes, IEnumerable<string> files)
        {
            //We want to get a hash of all the old files, this allows us to detect files which have moved
            foreach (var fileLocation in files)
            {
                await using var fileContentStream = File.OpenRead(fileLocation);
                var hash = _hasher.HashData(fileContentStream);
                if (!hashes.TryGetValue(hash, out var filesList))
                {
                    filesList = new List<string>();
                    hashes.Add(hash, filesList);
                }
            
                filesList.Add(fileLocation);
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
        
        hash ??= _hasher.HashData(fileContentStream);
        await AddHashAndSizeData(zipArchive, filepath, hash, fileContentStream.Length);

        return true;
    }

    private static void CheckFilePath(ref string filepath)
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