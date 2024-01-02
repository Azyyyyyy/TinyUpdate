using System.IO.Compression;
using NeoSmart.AsyncLock;
using SemVersion;
using TinyUpdate.Core;
using TinyUpdate.Core.Abstract;

namespace TinyUpdate.TUUP;

public class TuupUpdatePackageCreation(SHA256 sha256) : IUpdatePackageCreation
{
    private readonly AsyncLock _zipLock = new AsyncLock();

    public string Extension => Consts.Extension;

    public async Task<bool> CreateFullPackage(string applicationLocation, SemanticVersion applicationVersion, string updatePackageLocation,
        string applicationName, IProgress<double>? progress = null)
    {
        var files = Directory.GetFiles(applicationLocation, "*", SearchOption.AllDirectories);
        var zipArchive =
            CreateZipArchive(Path.Combine(updatePackageLocation, applicationName + "-" + applicationVersion + Extension));

        for (int i = 0; i < files.Length; i++)
        {
            var file = files[i];
            var fileContentStream = File.OpenRead(file);

            await AddNewFile(zipArchive, fileContentStream, Path.GetRelativePath(applicationLocation, file));
            
            await fileContentStream.DisposeAsync();
            progress?.Report((double)i / files.Length);
        }

        return true;
    }

    public Task<bool> CreateDeltaPackage(string oldApplicationLocation, SemanticVersion oldApplicationVersion,
        string newApplicationLocation, SemanticVersion newApplicationVersion, string updatePackageLocation,
        string applicationName, IProgress<double>? progress = null)
    {
        throw new NotImplementedException();
    }
    
    private static ZipArchive CreateZipArchive(string updatePackageLocation)
    {
        var updatePackageStream = File.Create(updatePackageLocation);
        return new ZipArchive(updatePackageStream, ZipArchiveMode.Create);
    }

    private Task<bool> AddNewFile(ZipArchive zipArchive, Stream fileContentStream, string filepath) =>
        AddFile(zipArchive, fileContentStream, filepath + ".new");

    private async Task<bool> AddFile(ZipArchive zipArchive, Stream fileContentStream, string filepath)
    {
        //Add the file
        using (await _zipLock.LockAsync())
        {
            var zipFileEntryStream = zipArchive.CreateEntry(filepath, CompressionLevel.SmallestSize).Open();
            await fileContentStream.CopyToAsync(zipFileEntryStream);
            await zipFileEntryStream.DisposeAsync();
        }
        
        var filesize = fileContentStream.Length;
        var sha256Hash = sha256.CreateSHA256Hash(fileContentStream);

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