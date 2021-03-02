using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using DeltaCompressionDotNet.MsDelta;
using TinyUpdate.Core;
using TinyUpdate.Core.Helper;
using TinyUpdate.Core.Logger;
using TinyUpdate.Core.Update;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Binary
{
    //TODO: Report back the progress
    //TODO: Create ApplyUpdate(UpdateInfo, Action<decimal>?)
    /// <summary>
    /// Applies updates that the <see cref="BinaryCreator"/> has created
    /// </summary>
    public class BinaryApplier : IUpdateApplier
    {
        private static readonly ILogging Logger = Logging.CreateLogger("BinaryApplier");
        private static readonly Dictionary<string, PatchType> PatchExtensions = new()
        {
            { ".bsdiff", PatchType.BSDiff },
            { ".diff", PatchType.MSDiff },
            { ".new", PatchType.New }
        };
        
        /// <inheritdoc cref="IUpdateApplier.ApplyUpdate(ReleaseEntry, Action{decimal})"/>
        public async Task<bool> ApplyUpdate(ReleaseEntry entry, Action<decimal>? progress = null)
        {
            //Get the paths for where the version will be and where the older version is
            var newPath = entry.Version.GetApplicationPath();
            var basePath = Global.ApplicationVersion.GetApplicationPath();

            /*Make sure the basePath exists, can't do a delta update without it!
             (This also shows that something is wrong)*/
            if (!Directory.Exists(basePath))
            {
                return false;
            }
            
            //Do the update!
            return await ApplyDeltaUpdate(basePath, newPath, entry, progress);
        }

        /// <inheritdoc cref="IUpdateApplier.ApplyUpdate(UpdateInfo, Action{decimal})"/>
        public Task<bool> ApplyUpdate(UpdateInfo updateInfo, Action<decimal>? progress = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="ApplyUpdate(ReleaseEntry, Action{decimal})"/>
        /// <param name="basePath">Path where we grab any old files that can be reused from</param>
        /// <param name="newPath">Where to put the new version of the application into</param>
        /// <param name="entry"></param>
        /// <param name="progress"></param>
        private static async Task<bool> ApplyDeltaUpdate(string basePath, string newPath, ReleaseEntry entry, Action<decimal>? progress = null)
        {
            if (!File.Exists(entry.FileLocation))
            {
                Logger.Error("Update file doesn't exist...");
                return false;
            }

            /*Create the folder that's going to contain this update
             deleting the folder if it already exists*/
            if (Directory.Exists(newPath))
            {
                Directory.Delete(newPath, true);
            }
            Directory.CreateDirectory(newPath);

            if (!entry.IsValidReleaseEntry(true))
            {
                Logger.Error("Update file doesn't match what we expect... Deleting update file and bailing");
                File.Delete(entry.FileLocation);
                return false;
            }

            //Grab all the files from the update file
            var zip = new ZipArchive(File.OpenRead(entry.FileLocation));
            var files = await GetFilesFromPackage(zip);
            if (files == null)
            {
                Logger.Error("Something happened while grabbing files in update file");
                /*This only happens when something is up with the update file
                  delete and return false*/
                zip.Dispose();
                File.Delete(entry.FileLocation);
                return false;
            }

            foreach (var file in files)
            {
                Logger.Debug("Processing {0}", file.FileLocation);
                
                //If we have a folder path then create it
                if (!string.IsNullOrWhiteSpace(file.FolderPath))
                {
                    Directory.CreateDirectory(Path.Combine(newPath, file.FolderPath));
                }

                //Get where the old and new file should be
                var originalFile = Path.Combine(basePath, file.FileLocation);
                var newFile = Path.Combine(newPath, file.FileLocation);

                /*Check that the files exists and that we was 
                  able to process file, if not then hard bail!*/
                var applySuccessful = 
                    (file.PatchType != PatchType.New && File.Exists(originalFile) ||
                     true) &&
                    await ProcessFile(originalFile, newFile, file);

                if (!applySuccessful || !CheckUpdatedFile(newFile, file))
                {
                    zip.Dispose();
                    file.Stream?.Dispose();
                    return false;
                }
                file.Stream?.Dispose();
            }

            zip.Dispose();
            return true;
        }

        /// <summary>
        /// Checks that the file applied is the file that should of been applied
        /// </summary>
        /// <param name="fileLocation">Where the file is</param>
        /// <param name="fileEntry">Details about the file</param>
        /// <returns>If it's what we expect</returns>
        private static bool CheckUpdatedFile(string fileLocation, FileEntry fileEntry)
        {
            //This file didn't change, assume it's fine
            if (fileEntry.Filesize == 0)
            {
                return true;
            }
            
            //Check file size and hash
            var fileStream = File.OpenRead(fileLocation);
            var isExpectedFile = 
                fileStream.Length == fileEntry.Filesize &&
                SHA1Util.CreateSHA1Hash(fileStream) == fileEntry.SHA1;
            fileStream.Dispose();

            /*Delete file if it's not expected, it's better for them to 
             reinstall then to catch a virus!*/
            if (!isExpectedFile)
            {
                File.Delete(fileLocation);
            }
            return isExpectedFile;
        }

        /// <summary>
        /// Processes a file that needs to be in the new version
        /// </summary>
        /// <param name="originalFile">Where the original file is</param>
        /// <param name="newFile">Where the file file needs to be</param>
        /// <param name="file">Details about how we the update was made</param>
        /// <returns>If we was able to process the file</returns>
        private static async Task<bool> ProcessFile(string originalFile, string newFile, FileEntry file)
        {
            //If the filesize isn't 0 then it means that the file was updated
            if (file.Filesize != 0)
            {
                return await (file.PatchType switch
                {
                    PatchType.MSDiff => ApplyMSDiffUpdate(file, newFile, originalFile),
                    PatchType.BSDiff => ApplyBSDiffUpdate(file, newFile, originalFile),
                    PatchType.New => CopyFile(file, newFile),
                    _ => Task.FromResult(false)
                });
            }

            /*If we got here then it means that we are working on a file that
             we *should* have, check that is the case*/
            if (!File.Exists(originalFile))
            {
                Logger.Error("File that we need to copy doesn't exist!");
                return false;
            }
            
            //Try to create the hard link
            if (HardLinkHelper.CreateHardLink(originalFile, newFile))
            {
                return true;
            }

            //We wasn't able to hard link, just try to copy the file
            try
            {
                File.Copy(originalFile, newFile);
            }
            catch (Exception e)
            {
                Logger.Error("Couldn't hard link or copy file!");
                Logger.Error(e);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Copies the file that was in the update file
        /// </summary>
        /// <param name="fileEntry">Entry with the file that needs to be copied</param>
        /// <param name="fileLocation">Where the file should go</param>
        /// <returns>If we was able to process the file</returns>
        private static async Task<bool> CopyFile(FileEntry fileEntry, string fileLocation)
        {
            var fileStream = File.OpenWrite(fileLocation);
            await fileEntry.Stream.CopyToAsync(fileStream);
            fileStream.Dispose();
            return true;
        }
        
        /// <summary>
        /// Applies a patch that was created using <see cref="MsDeltaCompression"/>
        /// </summary>
        /// <param name="fileEntry">Patch to apply</param>
        /// <param name="outputLocation">Where the output file should be</param>
        /// <param name="baseFile">Where the original file is</param>
        /// <returns>If the patch was applied correctly</returns>
        private static async Task<bool> ApplyMSDiffUpdate(FileEntry fileEntry, string outputLocation, string baseFile)
        {
            //Create Temp folder if it doesn't exist
            Directory.CreateDirectory(Global.TempFolder);
            
            var tmpFile = Path.Combine(Global.TempFolder, fileEntry.Filename);

            //Delete the tmp file if it already exists, likely from the last update
            if (File.Exists(tmpFile))
            {
                File.Delete(tmpFile);
            }

            //Put the delta file onto disk
            var tmpFileStream = File.OpenWrite(tmpFile);
            await fileEntry.Stream.CopyToAsync(tmpFileStream);
            tmpFileStream.Dispose();
            
            //Make the updated file!
            var msDelta = new MsDeltaCompression();
            try
            {
                File.Create(outputLocation).Dispose();
                msDelta.ApplyDelta(tmpFile, baseFile, outputLocation);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Logger.Error("File that failed to update: {0}", outputLocation);
                return false;
            }

            return true;
        }


        /// <summary>
        /// Applies a patch that was created using <see cref="BinaryPatchUtility"/>
        /// </summary>
        /// <inheritdoc cref="ApplyMSDiffUpdate(FileEntry, string, string)"/>
        private static async Task<bool> ApplyBSDiffUpdate(FileEntry fileEntry, string outputLocation, string baseFile)
        {
            //Create streams for old file and where the new file will be
            var outputStream = File.OpenWrite(outputLocation);
            var inputStream = StreamUtil.GetFileStreamRead(baseFile);
            if (inputStream == null)
            {
                return false;
            }
            
            //Create a memory stream as we really need to be able to seek
            var patchMemStream = new MemoryStream();
            await fileEntry.Stream.CopyToAsync(patchMemStream);
            var successfulUpdate = await BinaryPatchUtility.Apply(inputStream,  () =>
            {
                //Copy the files over in a memory stream
                var memStream = new MemoryStream();
                patchMemStream.Seek(0, SeekOrigin.Begin);
                patchMemStream.CopyTo(memStream);
                memStream.Seek(0, SeekOrigin.Begin);

                return memStream;
            }, outputStream);

            outputStream.Dispose();
            inputStream.Dispose();
            return successfulUpdate;
        }
        
        /// <summary>
        /// Gets all the files that this update will have and any information needed correctly apply the update
        /// </summary>
        /// <param name="zip"><see cref="ZipArchive"/> that contains all the files</param>
        private static async Task<IEnumerable<FileEntry>?> GetFilesFromPackage(ZipArchive zip)
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
                var filename = zipEntry.Name.Remove(zipEntry.Name.LastIndexOf(entryEtx, StringComparison.Ordinal));
                var filepath = Path.GetDirectoryName(zipEntry.FullName);

                //Get the index of the entry for adding new data (if it exists)
                var entryIndex = fileEntries.FindIndex(x => x.Filename == filename && x.FolderPath == filepath);
                
                //This means that the file is the binary that contains the patch, we want to get a stream to it
                if (PatchExtensions.ContainsKey(entryEtx))
                {
                    /*If we don't have the details about this update yet then just give the stream
                     we can always dispose of the stream if we find out that we don't need it*/
                    if (entryIndex == -1)
                    {
                        fileEntries.Add(new FileEntry(filename, filepath)
                        {
                            Stream = zipEntry.Open(),
                            PatchType = PatchExtensions[entryEtx]
                        });
                        continue;
                    }

                    //We know that we need the stream if the Filesize isn't 0
                    fileEntries[entryIndex].PatchType = PatchExtensions[entryEtx];
                    if (fileEntries[entryIndex].Filesize != 0)
                    {
                        fileEntries[entryIndex].Stream = zipEntry.Open();
                    }
                    continue;
                }
                
                /*This means that we will be finding any checking details
                 that we need to use when applying a patch (if this check returns false)*/
                if (entryEtx != ".shasum")
                {
                    continue;
                }

                var (sha1Hash, filesize) = await GetShasumDetails(zipEntry.Open());
                if (string.IsNullOrWhiteSpace(sha1Hash) || filesize == -1)
                {
                    /*If this happens then update file is not how it should be, clear all streams and return nothing*/
                    foreach (var fileEntry in fileEntries)
                    {
                        fileEntry.Stream?.Dispose();
                    }
                    return null;
                }

                //Update/Create FileEntry with hash and filesize
                if (entryIndex == -1)
                {
                    fileEntries.Add(new FileEntry(filename, filepath)
                    {
                        SHA1 = sha1Hash,
                        Filesize = filesize
                    });
                    continue;
                }

                fileEntries[entryIndex].SHA1 = sha1Hash;
                fileEntries[entryIndex].Filesize = filesize;
            }

            //Get rid of any streams that will be empty
            foreach (var fileEntry in fileEntries.Where(fileEntry => fileEntry.Filesize == 0))
            {
                fileEntry.Stream?.Dispose();
                fileEntry.Stream = null;
            }
            return fileEntries;
        }

        /// <summary>
        /// Gets the hash and filesize from a file that contains data about a file we need to use for updating
        /// </summary>
        /// <param name="fileStream">Stream of that file</param>
        /// <returns>SHA1 hash and filesize that is expected</returns>
        private static async Task<(string? sha1Hash, long filesize)> GetShasumDetails(Stream fileStream)
        {
            //Grab the text from the file
            var textStream = new StreamReader(fileStream);
            var text = await textStream.ReadToEndAsync();

            //Dispose the streams
            textStream.Dispose();
            fileStream.Dispose();

            //Return nothing if we don't have anything
            if (string.IsNullOrWhiteSpace(text))
            {
                return (null, -1);
            }

            //Grab what we need, checking that it's what we are expect
            var textS = text.Split(' ');
            var hash = textS[0];
            if (textS.Length != 2 ||
                string.IsNullOrWhiteSpace(hash) ||
                !SHA1Util.IsValidSHA1(hash) ||
                !long.TryParse(textS[1], out var filesize))
            {
                return (null, -1);
            }

            return (hash, filesize);
        }
    }
}