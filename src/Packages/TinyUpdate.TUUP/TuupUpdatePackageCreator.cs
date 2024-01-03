using System.IO.Abstractions;
using System.IO.Compression;
using NeoSmart.AsyncLock;
using SemVersion;
using TinyUpdate.Core;
using TinyUpdate.Core.Abstract;

namespace TinyUpdate.TUUP;

public class TuupUpdatePackageCreator : IUpdatePackageCreator
{
    private readonly AsyncLock _zipLock;
    private readonly SHA256 _sha256;
    private readonly IDeltaManager _deltaManager;
    private readonly TuupUpdatePackageCreatorOptions _options;
    
    // ReSharper disable InconsistentNaming
    private readonly IDirectory Directory;
    private readonly IFile File;
    // ReSharper restore InconsistentNaming

    public TuupUpdatePackageCreator(SHA256 sha256, IDeltaManager deltaManager, IDirectory directory, IFile file, TuupUpdatePackageCreatorOptions options)
    {
        _sha256 = sha256;
        _deltaManager = deltaManager;
        _options = options;
        Directory = directory;
        File = file;
        _zipLock = new AsyncLock();
    }

    public string Extension => Consts.Extension;

    public async Task<bool> CreateFullPackage(string applicationLocation, SemanticVersion applicationVersion, string updatePackageLocation,
        string applicationName, IProgress<double>? progress = null)
    {
        var files = Directory.GetFiles(applicationLocation, "*", SearchOption.AllDirectories);
        using var zipArchive =
            CreateZipArchive(Path.Combine(updatePackageLocation, applicationName + "-" + applicationVersion + Extension));

        for (int i = 0; i < files.Length; i++)
        {
            var fileLocation = files[i];
            await using var fileContentStream = File.OpenRead(fileLocation);

            await AddNewFile(zipArchive, fileContentStream, Path.GetRelativePath(applicationLocation, fileLocation));
            progress?.Report((double)i / files.Length);
        }

        return true;
    }

    //TODO: Handle "moved" files
    public async Task<bool> CreateDeltaPackage(string oldApplicationLocation, SemanticVersion oldApplicationVersion,
        string newApplicationLocation, SemanticVersion newApplicationVersion, string updatePackageLocation,
        string applicationName, IProgress<double>? progress = null)
    {
        var oldFiles = Directory.GetFiles(oldApplicationLocation, "*", SearchOption.AllDirectories);
        Dictionary<string, List<string>> oldFilesHashes = new();

        var newFiles = Directory.GetFiles(newApplicationLocation, "*", SearchOption.AllDirectories);
        Dictionary<string, List<string>> newFilesHashes = new();

        await GetHashes(oldFilesHashes, oldFiles);
        await GetHashes(newFilesHashes, newFiles);
        
        using var zipArchive =
            CreateZipArchive(Path.Combine(updatePackageLocation, applicationName + "-" + newApplicationVersion + Extension));

        for (int i = 0; i < newFiles.Length; ReportAndBump(ref i))
        {
            var newFile = newFiles[i];
            var newSha256Hash = newFilesHashes.First(x => x.Value.Any(y => y == newFile)).Key;
            var relativeNewFile = Path.GetRelativePath(newApplicationLocation, newFile);

            var oldFile = oldFiles.FirstOrDefault(x => x.EndsWith(relativeNewFile));

            await using var newFileContentStream = File.OpenRead(newFile);

            //See if we had the file in the older version
            if (string.IsNullOrWhiteSpace(oldFile))
            {
                //Try to find the file if it's moved 
                if (!await FindAndAddMovedFile(newSha256Hash, relativeNewFile, newFileContentStream.Length))
                {
                    await AddNewFile(zipArchive, newFileContentStream, relativeNewFile);
                }
                continue;
            }

            //See if the file is the same by the outputted hash
            var oldSha256Hash = oldFilesHashes.First(x => x.Value.Any(y => y == oldFile)).Key;
            if (newSha256Hash == oldSha256Hash)
            {
                await AddSameFile(zipArchive, relativeNewFile, newSha256Hash);
                continue;
            }
            
            //Again, see if the file has moved. Could have been renamed and another file has taken it's old filename
            if (await FindAndAddMovedFile(newSha256Hash, relativeNewFile, newFileContentStream.Length))
            {
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

        async Task<bool> FindAndAddMovedFile(string newSha256Hash, string relativeNewFile, long filesize)
        {
            //V1 Tuup format didn't have the concept of moved files, if we want to make v1 packages then don't handle this
            if (!_options.V1Compatible && oldFilesHashes.TryGetValue(newSha256Hash, out var oldFilesList) && oldFilesList.Count > 0)
            {
                var oldFile = oldFilesList[0];
                var filepath = relativeNewFile + ".moved";
                using (await _zipLock.LockAsync())
                {
                    await using var zipShasumEntryStream = zipArchive.CreateEntry(filepath, CompressionLevel.SmallestSize).Open();
                    await using var shasumStreamWriter = new StreamWriter(zipShasumEntryStream);
                    await shasumStreamWriter.WriteAsync(Path.GetRelativePath(oldApplicationLocation, oldFile));
                }

                await AddHashAndSizeData(zipArchive, filepath, newSha256Hash, filesize);

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
                var sha256Hash = _sha256.CreateSHA256Hash(oldFileContentStream);
                if (!hashes.TryGetValue(sha256Hash, out var filesList))
                {
                    filesList = new List<string>();
                    hashes.Add(sha256Hash, filesList);
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
        AddFile(zipArchive, fileContentStream, filepath + ".new");

    private Task<bool> AddSameFile(ZipArchive zipArchive, string filepath, string hash) =>
        AddFile(zipArchive, Stream.Null, filepath + ".diff", sha256Hash: hash);
    
    private async Task<bool> AddFile(ZipArchive zipArchive, Stream fileContentStream, string filepath, string? sha256Hash = null)
    {
        //Add the file
        using (await _zipLock.LockAsync())
        {
            var zipFileEntryStream = zipArchive.CreateEntry(filepath, CompressionLevel.SmallestSize).Open();
            await fileContentStream.CopyToAsync(zipFileEntryStream);
            await zipFileEntryStream.DisposeAsync();
        }
        
        sha256Hash ??= _sha256.CreateSHA256Hash(fileContentStream);
        await AddHashAndSizeData(zipArchive, filepath, sha256Hash, fileContentStream.Length);

        return true;
    }

    private async Task AddHashAndSizeData(ZipArchive zipArchive, string filepath, string sha256Hash, long filesize)
    {
        using (await _zipLock.LockAsync())
        {
            await using var zipShasumEntryStream = zipArchive.CreateEntry(Path.ChangeExtension(filepath, ".shasum"), CompressionLevel.SmallestSize).Open();
            await using var shasumStreamWriter = new StreamWriter(zipShasumEntryStream);
            await shasumStreamWriter.WriteAsync($"{sha256Hash} {filesize}");
        }
    }
}