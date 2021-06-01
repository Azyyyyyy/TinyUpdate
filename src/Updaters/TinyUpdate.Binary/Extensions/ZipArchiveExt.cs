using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using TinyUpdate.Binary.Delta;
using TinyUpdate.Binary.Entry;
using TinyUpdate.Core.Extensions;

namespace TinyUpdate.Binary.Extensions
{
    /// <summary>
    /// Extensions to make using <see cref="ZipArchive"/> easier
    /// </summary>
    public static class ZipArchiveExt
    {
        /// <summary>
        /// Creates a <see cref="UpdateEntry"/> from the contents of a <see cref="ZipArchive"/>
        /// </summary>
        /// <param name="zip"><see cref="ZipArchive"/> to grab data from</param>
        public static async Task<UpdateEntry?> CreateUpdateEntry(this ZipArchive zip)
        {
            var updateEntry = new UpdateEntry();
            await updateEntry.LoadAsyncEnumerable(GetFilesFromPackage(zip));
            return updateEntry;
        }

        /// <summary>
        /// Gets all the files that this update will have and any information needed correctly apply the update
        /// </summary>
        /// <param name="zip"><see cref="ZipArchive"/> that contains all the files</param>
        public static async IAsyncEnumerable<FileEntry?> GetFilesFromPackage(this ZipArchive zip)
        {
            var fileEntries = new List<FileEntry>();
            foreach (var zipEntry in zip.Entries)
            {
                //Get file extension, if it doesn't have one then we don't
                //want to deal with it as it's something we don't work with
                var entryEtx = Path.GetExtension(zipEntry.Name);
                if (string.IsNullOrEmpty(entryEtx))
                {
                    continue;
                }

                //Get the filename + path so we can find the entry if it exists (or create if it doesn't)
                var filename =
                    zipEntry.Name.Substring(0, zipEntry.Name.LastIndexOf(entryEtx, StringComparison.Ordinal));
                var filepath = Path.GetDirectoryName(zipEntry.FullName);

                //Get the index of the entry for adding new data (if it exists)
                var entryIndex = fileEntries.IndexOf(x => x?.Filename == filename && x.FolderPath == filepath);

                //This means that the file is the binary that contains the patch, we want to get a stream to it
                if (entryEtx == ".new"
                    || DeltaCreation.DeltaUpdaters.Any(x => x.Extension == entryEtx))
                {
                    /*If we don't have the details about this update yet then just give the stream
                     we can always dispose of the stream if we find out that we don't need it*/
                    if (entryIndex == -1)
                    {
                        fileEntries.Add(new FileEntry(filename, filepath)
                        {
                            Stream = zipEntry.Open(),
                            DeltaExtension = entryEtx
                        });
                        continue;
                    }

                    //We know that we need the stream if the Filesize isn't 0
                    fileEntries[entryIndex].DeltaExtension = entryEtx;
                    if (fileEntries[entryIndex].Filesize != 0)
                    {
                        fileEntries[entryIndex].Stream = zipEntry.Open();
                    }

                    yield return fileEntries[entryIndex];
                    fileEntries.RemoveAt(entryIndex);
                    continue;
                }

                //If its this then it will be the loader for the application
                if (entryEtx == ".load")
                {
                    fileEntries.Add(new FileEntry(filename, filepath)
                    {
                        Stream = zipEntry.Open(),
                        DeltaExtension = entryEtx
                    });
                    continue;
                }

                /*This means that we will be finding any checking details
                 that we need to use when applying a patch (if this check returns false)*/
                if (entryEtx != ".shasum")
                {
                    continue;
                }

                var (sha256Hash, filesize) = await zipEntry.Open().GetShasumDetails();
                if (string.IsNullOrWhiteSpace(sha256Hash) || filesize == -1)
                {
                    /*If this happens then update file is not how it should be, clear all streams and return nothing*/
                    foreach (var fileEntry in fileEntries)
                    {
                        fileEntry.Stream?.Dispose();
                    }

                    yield break;
                }

                //Update/Create FileEntry with hash and filesize
                if (entryIndex == -1)
                {
                    fileEntries.Add(new FileEntry(filename, filepath)
                    {
                        SHA256 = sha256Hash,
                        Filesize = filesize
                    });
                    continue;
                }

                fileEntries[entryIndex].SHA256 = sha256Hash;
                fileEntries[entryIndex].Filesize = filesize;

                //Clear stream if we don't need it
                if (filesize == 0)
                {
                    fileEntries[entryIndex].Stream?.Dispose();
                    fileEntries[entryIndex].Stream = null;
                }

                yield return fileEntries[entryIndex];
                fileEntries.RemoveAt(entryIndex);
            }

            //Make sure that everything that wasn't returned gets returned (as long it has the data needed)
            var fileEntriesCounter = 0;
            foreach (var fileEntry in fileEntries.Where(fileEntry =>
                fileEntry.Filesize == 0 || !string.IsNullOrWhiteSpace(fileEntry.SHA256)))
            {
                fileEntriesCounter++;
                if (fileEntry.Filesize == 0)
                {
                    fileEntry.Stream?.Dispose();
                    fileEntry.Stream = null;
                }

                yield return fileEntry;
            }

            Debug.Assert(fileEntries.Count - fileEntriesCounter == 0);
        }
    }
}