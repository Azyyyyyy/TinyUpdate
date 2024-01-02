﻿using System.IO.Compression;
using TinyUpdate.Core;
using TinyUpdate.Core.Abstract;

namespace TinyUpdate.TUUP;

/// <summary>
///     Update Package based on TinyUpdate V1, Makes use of zip format
/// </summary>
public class TuupUpdatePackage(IDeltaManager deltaManager, SHA256 sha256) : IUpdatePackage
{
    private bool _loaded;
    private ZipArchive? _zipArchive;
    private static readonly string[] ExpectedData = ["Filename", "Path", "SHA256", "Filesize", "Extension"];

    public string Extension => Consts.Extension;

    public async Task Load(Stream updatePackageStream)
    {
        try
        {
            _zipArchive = new ZipArchive(updatePackageStream, ZipArchiveMode.Read);
        }
        catch (Exception e)
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

    public ICollection<FileEntry> DeltaFiles { get; } = new List<FileEntry>();
    public ICollection<FileEntry> UnchangedFiles { get; } = new List<FileEntry>();
    public ICollection<FileEntry> NewFiles { get; } = new List<FileEntry>();
    public ICollection<FileEntry> MovedFiles { get; } = new List<FileEntry>();

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
            var entryEtx = Path.GetExtension(zipEntry.Name);
            if (string.IsNullOrEmpty(entryEtx))
            {
                continue;
            }

            //We append the an extension to tell the system how to handle the file, remove it so we get the actual filename
            var filename =
                zipEntry.Name[..zipEntry.Name.LastIndexOf(entryEtx, StringComparison.Ordinal)];
            var filepath = Path.GetDirectoryName(zipEntry.FullName) ?? "";

            var key = Path.Combine(filepath, filename); //So we can store data in the same place

            if (!fileEntriesData.TryGetValue(key, out var fileEntryData))
            {
                fileEntryData = new Dictionary<string, object?>();
                fileEntriesData.Add(key, fileEntryData);
                fileEntryData["Filename"] = filename;
                fileEntryData["Path"] = filepath;
            }

            switch (entryEtx)
            {
                case ".moved":
                {
                    using var textStream = new StreamReader(zipEntry.Open());
                    var text = await textStream.ReadToEndAsync();

                    fileEntryData["PreviousLocation"] = text;
                    break;
                }
                case ".shasum":
                {
                    var (sha256Hash, filesize) = await zipEntry.Open().GetShasumDetails(sha256);
                    fileEntryData["SHA256"] = sha256Hash;
                    fileEntryData["Filesize"] = filesize;
                
                    //Clear out stream if it'll be nothing, no need to keep it
                    if (filesize == 0 && fileEntryData.TryGetValue("Stream", out var streamObj))
                    {
                        await ((Stream)streamObj!).DisposeAsync();
                        fileEntryData["Stream"] = null!;
                    }

                    break;
                }
            }
            
            //This means that this entry contains data we want to work with 
            if (entryEtx == ".new" || entryEtx == ".moved"
                || deltaManager.Appliers.Any(x => x.Extension == entryEtx))
            {
                fileEntryData["Stream"] = zipEntry.Open();
                fileEntryData["Extension"] = entryEtx;
            }
        }

        //Now that we got all the information needed, lets throw it back!
        foreach (var (_, fileEntryData) in fileEntriesData)
        {
            if (HasAllFileEntryData(fileEntryData))
            {
                yield return new FileEntry((string)fileEntryData["Filename"]!, (string)fileEntryData["Path"]!)
                {
                    SHA256 = (string)fileEntryData["SHA256"]!,
                    Filesize = (long)fileEntryData["Filesize"]!,
                    Stream = (Stream?)fileEntryData["Stream"],
                    Extension = (string)fileEntryData["Extension"]!,
                    PreviousLocation = (string?)fileEntryData["PreviousLocation"]
                };
            }
        }
    }

    private bool HasAllFileEntryData(Dictionary<string, object?> fileEntryData)
    {
        var missingCore = ExpectedData.Except(fileEntryData.Keys).Any();
        if (missingCore)
        {
            return false;
        }

        if ((long?)fileEntryData["Filesize"] != 0 && !fileEntryData.ContainsKey("Stream"))
        {
            return false;
        }

        fileEntryData.TryAdd("PreviousLocation", null);
        return true;
    }
}