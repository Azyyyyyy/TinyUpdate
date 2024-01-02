using System.IO.Compression;
using System.Runtime.CompilerServices;
using NeoSmart.AsyncLock;
using SemVersion;
using TinyUpdate.Core;
using TinyUpdate.Core.Abstract;

namespace TinyUpdate.TUUP;

public class TuupUpdatePackageCreation(SHA256 sha256, IDeltaManager deltaManager) : IUpdatePackageCreation
{
    private readonly AsyncLock _zipLock = new AsyncLock();

    public string Extension => Consts.Extension;

    public async Task<bool> CreateFullPackage(string applicationLocation, SemanticVersion applicationVersion, string updatePackageLocation,
        string applicationName, IProgress<double>? progress = null)
    {
        var files = Directory.GetFiles(applicationLocation, "*", SearchOption.AllDirectories);
        using var zipArchive =
            CreateZipArchive(Path.Combine(updatePackageLocation, applicationName + "-" + applicationVersion + Extension));

        for (int i = 0; i < files.Length; i++)
        {
            var file = files[i];
            await using var fileContentStream = File.OpenRead(file);

            await AddNewFile(zipArchive, fileContentStream, Path.GetRelativePath(applicationLocation, file));
            
            await fileContentStream.DisposeAsync();
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
        var newFiles = Directory.GetFiles(newApplicationLocation, "*", SearchOption.AllDirectories);
        using var zipArchive =
            CreateZipArchive(Path.Combine(updatePackageLocation, applicationName + "-" + newApplicationVersion + Extension));

        for (int i = 0; i < newFiles.Length; ReportAndBump(ref i))
        {
            var newFile = newFiles[i];
            var relNewFile = Path.GetRelativePath(newApplicationLocation, newFile);
            var oldFile = oldFiles.FirstOrDefault(x => x.EndsWith(relNewFile));

            await using var newFileContentStream = File.OpenRead(newFile);

            //See if we had the file in the older version
            if (string.IsNullOrWhiteSpace(oldFile))
            {
                await AddNewFile(zipArchive, newFileContentStream, relNewFile);
                continue;
            }

            //See if the file is the same by the outputted hash
            await using var oldFileContentStream = File.OpenRead(oldFile);
            var newSha256Hash = sha256.CreateSHA256Hash(newFileContentStream);
            var oldSha256Hash = sha256.CreateSHA256Hash(oldFileContentStream);
            
            if (newSha256Hash == oldSha256Hash)
            {
                await AddSameFile(zipArchive, relNewFile, newSha256Hash);
                continue;
            }

            newFileContentStream.Seek(0, SeekOrigin.Begin);
            oldFileContentStream.Seek(0, SeekOrigin.Begin);
            
            var deltaResult = await deltaManager.CreateDeltaFile(oldFileContentStream, newFileContentStream);
            if (deltaResult.Successful)
            {
                deltaResult.DeltaStream.Seek(0, SeekOrigin.Begin);
                
                await AddFile(
                    zipArchive,
                    deltaResult.DeltaStream,
                    relNewFile + deltaResult.Creator.Extension);
            }
        }
        
        return true;

        void ReportAndBump(ref int value)
        {
            progress?.Report((double)value / newFiles.Length); 
            value++;
        }
    }
    
    private static ZipArchive CreateZipArchive(string updatePackageLocation)
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
        
        var filesize = fileContentStream.Length;
        sha256Hash ??= sha256.CreateSHA256Hash(fileContentStream);

        //Add the file details (size + hash)
        using (await _zipLock.LockAsync())
        {
            await using var zipShasumEntryStream = zipArchive.CreateEntry(filepath + ".shasum", CompressionLevel.SmallestSize).Open();
            await using var shasumStreamWriter = new StreamWriter(zipShasumEntryStream);
            await shasumStreamWriter.WriteAsync($"{sha256Hash} {filesize}");
        }

        return true;
    }
}