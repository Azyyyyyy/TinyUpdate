using System.IO.Compression;
using TinyUpdate.Core;
using TinyUpdate.Core.Abstract;
using TinyUpdate.Core.Model;

namespace TinyUpdate.TUUP;

/// <summary>
///     Update Package based on TinyUpdate V1 (With some extra functionally)
/// </summary>
public class TuupUpdatePackage(IDeltaManager deltaManager, IHasher hasher) : IUpdatePackage, IDisposable
{
    //What we expect to be contained for every file within the update package
    private static readonly string[] ExpectedData = ["Filename", "Path", "Hash", "Filesize", "Extension"];

    private bool _loaded;
    private bool _disposed;
    private ZipArchive? _zipArchive;

    public string Extension => Consts.TuupExtension;
    public ICollection<FileEntry> DeltaFiles { get; } = new List<FileEntry>();
    public ICollection<FileEntry> UnchangedFiles { get; } = new List<FileEntry>();
    public ICollection<FileEntry> NewFiles { get; } = new List<FileEntry>();
    public ICollection<FileEntry> MovedFiles { get; } = new List<FileEntry>();

    public async Task Load(Stream updatePackageStream)
    {
        try
        {
            _zipArchive = new ZipArchive(updatePackageStream, ZipArchiveMode.Read);
        }
        catch (InvalidDataException e)
        {
            throw new InvalidDataException("TuupUpdatePackage expects a zip formatted package", e);
        }
        
        await foreach (var fileEntry in GetFilesFromPackage(_zipArchive))
        {
            //Add to the correct collection 
            if (fileEntry.IsDeltaFile())
            {
                DeltaFiles.Add(fileEntry);
                continue;
            }

            if (fileEntry.IsNewFile())
            {
                NewFiles.Add(fileEntry);
                continue;
            }

            if (fileEntry.HasFileMoved())
            {
                MovedFiles.Add(fileEntry);
                continue;
            }
            
            UnchangedFiles.Add(fileEntry);
        }

        _loaded = true;
    }
    
    /// <summary>
    ///     Gets all the files that this update will have and any information needed to correctly apply the update
    /// </summary>
    /// <param name="zip"><see cref="ZipArchive" /> that contains all the files</param>
    private async IAsyncEnumerable<FileEntry> GetFilesFromPackage(ZipArchive zip)
    {
        var fileEntriesData = new Dictionary<string, Dictionary<string, object?>>(zip.Entries.Count);
        foreach (var zipEntry in zip.Entries)
        {
            //Check if the name contains a extension
            var extension = Path.GetExtension(zipEntry.Name);
            if (string.IsNullOrEmpty(extension))
            {
                continue;
            }

            //We append the extension so the system knows how to handle the file, we want to remove it so we get the actual filename
            var filename = zipEntry.Name[..zipEntry.Name.LastIndexOf(extension, StringComparison.Ordinal)];
            var filepath = Path.GetDirectoryName(zipEntry.FullName) ?? "";
            var key = Path.Combine(filepath, filename); //So we can store data in the same place

            if (!fileEntriesData.TryGetValue(key, out var fileEntryData))
            {
                fileEntryData = new Dictionary<string, object?>();
                fileEntriesData.Add(key, fileEntryData);
                fileEntryData["Filename"] = filename;
                fileEntryData["Path"] = filepath;
            }

            switch (extension)
            {
                case Consts.MovedFileExtension:
                {
                    using var textStream = new StreamReader(zipEntry.Open());
                    var text = await textStream.ReadToEndAsync();

                    fileEntryData["PreviousLocation"] = text;
                    break;
                }
                case Consts.ShasumFileExtension:
                {
                    var (hash, filesize) = await zipEntry.Open().GetShasumDetails(hasher);
                    fileEntryData["Hash"] = hash;
                    fileEntryData["Filesize"] = filesize;
                    break;
                }
            }
            
            //This means that this entry contains data we want to work with 
            if (extension == Consts.NewFileExtension
                || deltaManager.Appliers.Any(x => x.Extension == extension))
            {
                fileEntryData["Stream"] = zipEntry.Open();
                fileEntryData["Extension"] = extension;
            }

            if (extension is Consts.UnchangedFileExtension or Consts.MovedFileExtension)
            {
                fileEntryData["Extension"] = extension;
            }
        }

        //TODO: See if they is a way that we could make this without having to cast everything, this works for now
        //Now that we got all the information needed, lets throw it back!
        foreach (var (_, fileEntryData) in fileEntriesData)
        {
            if (HasAllFileEntryData(fileEntryData))
            {
                yield return new FileEntry((string)fileEntryData["Filename"]!, (string)fileEntryData["Path"]!)
                {
                    Hash = (string)fileEntryData["Hash"]!,
                    Filesize = (long)fileEntryData["Filesize"]!,
                    Stream = (Stream?)fileEntryData["Stream"],
                    Extension = (string)fileEntryData["Extension"]!,
                    PreviousLocation = (string?)fileEntryData["PreviousLocation"]
                };
            }
        }
    }

    private static bool HasAllFileEntryData(Dictionary<string, object?> fileEntryData)
    {
        var missingCore = ExpectedData.Except(fileEntryData.Keys).Any();
        if (missingCore)
        {
            return false;
        }

        //TODO: Maybe relax this restriction?
        //Check if we've got a file without any data when data is expected!
        var filesize = (long?)fileEntryData["Filesize"];
        if (filesize != 0 
            && fileEntryData["Extension"] is not Consts.UnchangedFileExtension and not Consts.MovedFileExtension 
            && !fileEntryData.ContainsKey("Stream"))
        {
            return false;
        }
        
        //Clear out the stream if it'll be nothing, no need to keep it
        if (filesize == 0 && fileEntryData.TryGetValue("Stream", out var streamObj))
        {
            ((Stream?)streamObj)?.Dispose();
            fileEntryData["Stream"] = null;
        }

        //Possible that these will be nothing, still need something for them
        fileEntryData.TryAdd("Stream", null);
        fileEntryData.TryAdd("PreviousLocation", null);
        return true;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_loaded)
            {
                _zipArchive?.Dispose();
                _loaded = false;
            }
            
            GC.SuppressFinalize(this);
            _disposed = true;
        }
    }
}