using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using DeltaCompressionDotNet.MsDelta;
using TinyUpdate.Core;
using TinyUpdate.Core.Helper;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Update;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Binary
{
    //TODO: Check OS being ran in MSDiff
    /// <summary>
    /// Applies updates that the <see cref="BinaryCreator"/> has created
    /// </summary>
    public class BinaryApplier : IUpdateApplier
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger("BinaryApplier");
        private static readonly Dictionary<string, PatchType> PatchExtensions = new()
        {
            { ".bsdiff", PatchType.BSDiff },
            { ".diff", PatchType.MSDiff },
            { ".new", PatchType.New }
        };
        
        /// <inheritdoc cref="IUpdateApplier.ApplyUpdate(ReleaseEntry, Action{decimal})"/>
        public async Task<bool> ApplyUpdate(ReleaseEntry entry, Action<decimal>? progress = null)
        {
            //Check that we was the one who made the update (as shown with the file extension)
            if (Path.GetExtension(entry.Filename) != Global.TinyUpdateExtension)
            {
                Logger.Error("{0} is not a update made by {1}, bail...", entry.FileLocation, GetType().Name);
                return false;
            }
            
            //Get the paths for where the version will be and where the older version is
            var newPath = entry.Version.GetApplicationPath();
            var basePath = Global.ApplicationVersion.GetApplicationPath();

            /*Make sure the basePath exists, can't do a delta update without it!
             (This also shows that something is *REALLY* wrong)*/
            if (!Directory.Exists(basePath))
            {
                Logger.Error("{0} doesn't exist, can't update", basePath);
                return false;
            }

            /*Make sure that the folder doesn't exist, likely that application closed
             down while updating last time*/
            DeleteFolder(newPath);
            
            //Do the update!
            return await ApplyUpdate(basePath, newPath, entry, progress);
        }

        /// <inheritdoc cref="IUpdateApplier.ApplyUpdate(UpdateInfo, Action{decimal})"/>
        public async Task<bool> ApplyUpdate(UpdateInfo updateInfo, Action<decimal>? progress = null)
        {
            if (!updateInfo.HasUpdate)
            {
                Logger.Error("We don't have a update to apply!!");
                return false;
            }
            
            var newVersionFolder = updateInfo.NewVersion?.GetApplicationPath();
            if (string.IsNullOrWhiteSpace(newVersionFolder))
            {
                Logger.Error("Can't get folder for the new version");
                return false;
            }
            DeleteFolder(newVersionFolder);

            //TODO: Make it so we don't process any files that aren't in the newest version of the application
            var updateCounter = 0m;
            var updateCount = updateInfo.Updates.Count();
            var doneFirstUpdate = false;
            foreach (var updateEntry in updateInfo.Updates)
            {
                if (!await ApplyUpdate(
                    doneFirstUpdate ? newVersionFolder : Global.ApplicationVersion.GetApplicationPath(),
                    newVersionFolder,
                    updateEntry,
                    updateProgress => progress?.Invoke((updateProgress + updateCounter) / updateCount)))
                {
                    Logger.Error("Applying version {0} failed", updateEntry.Version);
                    return false;
                }
                updateCounter++;

                doneFirstUpdate = true;
            }
            
            return true;
        }

        private static void DeleteFolder(string folder)
        {
            /*Create the folder that's going to contain this update
             deleting the folder if it already exists*/
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
            Directory.CreateDirectory(folder);
        }

        /// <inheritdoc cref="ApplyUpdate(ReleaseEntry, Action{decimal})"/>
        /// <param name="basePath">Path where we grab any old files that can be reused from</param>
        /// <param name="newPath">Where to put the new version of the application into</param>
        /// <param name="entry"></param>
        /// <param name="progress"></param>
        private static async Task<bool> ApplyUpdate(string basePath, string newPath, ReleaseEntry entry, Action<decimal>? progress = null)
        {
            if (!File.Exists(entry.FileLocation))
            {
                Logger.Error("Update file doesn't exist...");
                return false;
            }
            
            if (!entry.IsValidReleaseEntry(true))
            {
                Logger.Error("Update file doesn't match what we expect... deleting update file and bailing");
                File.Delete(entry.FileLocation);
                return false;
            }

            //Grab all the files from the update file
            var zip = new ZipArchive(File.OpenRead(entry.FileLocation));
            var files = (await GetFilesFromPackage(zip))?.ToArray();
            if (files == null)
            {
                /*This only happens when something is up with the update file,
                  delete and return false*/
                Logger.Error("Something happened while grabbing files in update file... deleting update file and bailing");
                zip.Dispose();
                File.Delete(entry.FileLocation);
                return false;
            }

            var fileCounter = 0L;
            var filesCount = files.LongCount();

            //We want to do new and delta files last
            var deltaFiles = new List<FileEntry>();
            var newFiles = new List<FileEntry>();
            foreach (var file in files)
            {
                //If we have a folder path then create it
                if (!string.IsNullOrWhiteSpace(file.FolderPath))
                {
                    var folder = Path.Combine(newPath, file.FolderPath);
                    Logger.Debug("Creating folder {0}", folder);
                    Directory.CreateDirectory(folder);
                }
                
                //See if this file is a delta/new file. If so then do it after
                if (IsDeltaFile(file))
                {
                    deltaFiles.Add(file);
                    continue;
                }
                if (IsNewFile(file))
                {
                    newFiles.Add(file);
                    continue;
                }
                
                //If we get here then it's the same file
                fileCounter++;
                Logger.Debug("Processing {0}", file.FileLocation);

                //Get where the old and "new" file should be
                var originalFile = Path.Combine(basePath, file.FileLocation);
                var newFile = Path.Combine(newPath, file.FileLocation);

                /*Check that the files exists and that we was 
                  able to process file, if not then hard bail!*/
                if (!ProcessSameFile(originalFile, newFile))
                {
                    zip.Dispose();
                    file.Stream?.Dispose();
                    progress?.Invoke(1);
                    return false;
                }

                //We need that extra 1 so we are at 99% when done (we got some cleanup to do after)
                progress?.Invoke((decimal)fileCounter / (filesCount + 1));
                file.Stream?.Dispose();
            }

            //Note that this should be the only loop that is used when doing a full update
            foreach (var newFile in newFiles)
            {
                fileCounter++;
                Logger.Debug("Processing {0}", newFile.FileLocation);

                var newFileLocation = Path.Combine(newPath, newFile.FileLocation);
                var applySuccessful = await ProcessNewFile(newFile, newFileLocation);
                
                if (!applySuccessful || !CheckUpdatedFile(newFileLocation, newFile))
                {
                    zip.Dispose();
                    newFile.Stream?.Dispose();
                    progress?.Invoke(1);
                    return false;
                }

                //We need that extra 1 so we are at 99% when done (we got some cleanup to do after)
                progress?.Invoke((decimal)fileCounter / (filesCount + 1));
                newFile.Stream?.Dispose();
            }
            
            foreach (var deltaFile in deltaFiles)
            {
                fileCounter++;
                Logger.Debug("Processing {0}", deltaFile.FileLocation);

                var originalFile = Path.Combine(basePath, deltaFile.FileLocation);
                var newFileLocation = Path.Combine(newPath, deltaFile.FileLocation);
                var applySuccessful = await ProcessDeltaFile(originalFile, newFileLocation, deltaFile, 
                    obj => progress?.Invoke((fileCounter + obj) / (filesCount + 1)));
                
                if (!applySuccessful || !CheckUpdatedFile(newFileLocation, deltaFile))
                {
                    zip.Dispose();
                    deltaFile.Stream?.Dispose();
                    progress?.Invoke(1);
                    return false;
                }

                //We need that extra 1 so we are at 99% when done (we got some cleanup to do after)
                progress?.Invoke((decimal)fileCounter / (filesCount + 1));
                deltaFile.Stream?.Dispose();
            }

            zip.Dispose();
            progress?.Invoke(1);
            return true;
        }

        /// <summary>
        /// Checks that this file is a delta file 
        /// </summary>
        /// <param name="fileEntry">Details about the file</param>
        private static bool IsDeltaFile(FileEntry fileEntry)
        {
            return fileEntry.Filesize != 0 && fileEntry.PatchType != PatchType.New;
        }

        /// <summary>
        /// Checks that this file is a new file 
        /// </summary>
        /// <param name="fileEntry">Details about the file</param>
        private static bool IsNewFile(FileEntry fileEntry)
        {
            return fileEntry.Filesize != 0 && fileEntry.PatchType == PatchType.New;
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
                Logger.Debug("Filesize is 0, we should be fine");
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
                Logger.Error("Updated file is not what we expect, deleting it!");
                File.Delete(fileLocation);
            }
            return isExpectedFile;
        }

        /// <summary>
        /// Processes a file that has a delta update
        /// </summary>
        /// <param name="originalFile">Where the original file is</param>
        /// <param name="newFile">Where the file file needs to be</param>
        /// <param name="file">Details about how we the update was made</param>
        /// <param name="progress">Progress of applying update</param>
        /// <returns>If we was able to process the file</returns>
        private static async Task<bool> ProcessDeltaFile(string originalFile, string newFile, FileEntry file, Action<decimal>? progress = null)
        {
            //If the filesize isn't 0 then it means that the file was updated
            Logger.Debug("File was updated, apply delta update");
            return await (file.PatchType switch
            {
                PatchType.MSDiff => ApplyMSDiffUpdate(file, newFile, originalFile),
                PatchType.BSDiff => ApplyBSDiffUpdate(file, newFile, originalFile, progress),
                _ => Task.FromResult(false)
            });
        }

        /// <summary>
        /// Processes a file that is the same in both versions
        /// </summary>
        /// <param name="originalFile">Where the original file is located</param>
        /// <param name="newFile">Where the "new" file will be</param>
        /// <returns>If we was able to process the file</returns>
        private static bool ProcessSameFile(string originalFile, string newFile)
        {
            /*If we got here then it means that we are working on a file that
             we *should* have, check that is the case*/
            Logger.Debug("File wasn't updated, making sure file exists and then making hard link");
            if (!File.Exists(originalFile))
            {
                Logger.Error("File that we need to copy doesn't exist!");
                return false;
            }

            //We already put the file in place from another update
            if (newFile == originalFile)
            {
                return true;
            }

            if (File.Exists(newFile))
            {
                File.Delete(newFile);                
            }
            
            //Try to create the hard link
            if (HardLinkHelper.CreateHardLink(originalFile, newFile))
            {
                return true;
            }

            //We wasn't able to hard link, just try to copy the file
            Logger.Warning("Wasn't able to create hard link, just going to copy the file");
            try
            {
                File.Copy(originalFile, newFile);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Couldn't hard link or copy file!");
                Logger.Error(e);
                return false;
            }
        }
        
        /// <summary>
        /// Copies the file that was in the update file
        /// </summary>
        /// <param name="fileEntry">Entry with the file that needs to be copied</param>
        /// <param name="fileLocation">Where the file should go</param>
        /// <returns>If we was able to process the file</returns>
        private static async Task<bool> ProcessNewFile(FileEntry fileEntry, string fileLocation)
        {
            if (fileEntry.Stream == null)
            {
                Logger.Error("fileEntry's doesn't have a stream, can't copy file that would be at {0}", fileLocation);
                return false;
            }
            
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

            if (fileEntry.Stream == null)
            {
                Logger.Error("fileEntry's doesn't have a stream, can't make MSDiff update");
                return false;
            }
            
            //Put the delta file onto disk
            var tmpFileStream = File.OpenWrite(tmpFile);
            await fileEntry.Stream.CopyToAsync(tmpFileStream);
            tmpFileStream.Dispose();
            
            //If baseFile + outputLocation are the same, copy it to a tmp file
            //and then give it that (deleting it after)
            if (baseFile == outputLocation)
            {
                var tmpBaseFile = Path.Combine(Global.TempFolder, Path.GetRandomFileName());
                File.Copy(baseFile, tmpBaseFile);
                baseFile = tmpBaseFile;
            }
            
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
        /// <param name="progress">Progress of applying update</param>
        /// <param name="fileEntry"></param>
        /// <param name="outputLocation"></param>
        /// <param name="baseFile"></param>
        private static async Task<bool> ApplyBSDiffUpdate(FileEntry fileEntry, string outputLocation, string baseFile, Action<decimal> progress)
        {
            Stream? inputStream = null;
            /*If this is the same file then we want to copy it to mem and not
             read from disk*/
            if (outputLocation == baseFile)
            {
                inputStream = new MemoryStream();
                var fileStream = StreamUtil.GetFileStreamRead(baseFile);
                if (fileStream == null)
                {
                    Logger.Error("Wasn't able to grab {0} for applying a BSDiff update", baseFile);
                    return false;
                }
                await fileStream.CopyToAsync(inputStream);
                fileStream.Dispose();
                inputStream.Seek(0, SeekOrigin.Begin);
            }
            
            //and put it into a memorystream so when we override, we still have data
            inputStream ??= StreamUtil.GetFileStreamRead(baseFile);

            //Create streams for old file and where the new file will be
            var outputStream = File.OpenWrite(outputLocation);
            
            //Check streams that can be null
            if (inputStream == null)
            {
                Logger.Error("Wasn't able to grab {0} for applying a BSDiff update", baseFile);
                return false;
            }
            
            if (fileEntry.Stream == null)
            {
                Logger.Error("fileEntry's doesn't have a stream, can't make BSDiff update");
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