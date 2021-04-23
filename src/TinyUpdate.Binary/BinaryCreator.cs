using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TinyUpdate.Binary.Delta;
using TinyUpdate.Binary.Extensions;
using TinyUpdate.Core;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Update;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Binary
{
    //TODO: Add a intended OS opt to allow us to not do stuff like MSDiff if making patch for another OS
    /// <summary>
    /// Creates update files in a binary format 
    /// </summary>
    public class BinaryCreator : IUpdateCreator
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(BinaryCreator));

        /// <inheritdoc cref="IUpdateCreator.Extension"/>
        public string Extension { get; } = ".tuup";
        
        /// <inheritdoc cref="IUpdateCreator.CreateDeltaPackage"/>
        public async Task<bool> CreateDeltaPackage(
            string newVersionLocation, 
            string baseVersionLocation, 
            string? deltaUpdateLocation = null, 
            int concurrentDeltaCreation = 1,
            Action<decimal>? progress = null)
        {
            //To keep track of progress
            long fileCount = 0;
            long deltaFilesLength = 0;
            long newFilesLength;
            long sameFilesLength;
            decimal lastProgress = 0;
            
            void UpdateProgress(decimal extraProgress = 0)
            {
                //We need that extra 1 so we are at 99% when done (we got some cleanup to do after)
                var progressValue = (fileCount - sameFilesLength + extraProgress) / (deltaFilesLength + newFilesLength);
                if (progressValue != lastProgress)
                {
                    progress?.Invoke(progressValue);
                    lastProgress = progressValue;
                }
            }
            
            if (!Directory.Exists(newVersionLocation) || 
                !Directory.Exists(baseVersionLocation))
            {
                Logger.Error("One of the folders don't exist, can't create....");
                return false;
            }
            
            Logger.Debug("Creating delta file");
            var zipArchive = CreateZipArchive(deltaUpdateLocation);

            void Cleanup()
            {
                zipArchive.Dispose();
                progress?.Invoke(1);
            }
            
            //Get all the files that are in the new version (Removing the Path so we only have the relative path of the file)
            var newVersionFiles = Directory.EnumerateFiles(newVersionLocation, "*", SearchOption.AllDirectories)
                .RemovePath(newVersionLocation).ToArray();
            
            //and get the files from the old version
            var baseVersionFiles = Directory.EnumerateFiles(baseVersionLocation, "*", SearchOption.AllDirectories)
                .RemovePath(baseVersionLocation).ToArray();

            //Find any files that are in both version and process them based on if they had any changes
            Logger.Information("Processing files that are in both versions");
            var sameFiles = newVersionFiles.Where(x => baseVersionFiles.Contains(x)).ToArray();
            var newFiles = newVersionFiles.Where(x => !sameFiles.Contains(x)).ToArray();

            newFilesLength = newFiles.LongLength;
            sameFilesLength = sameFiles.LongLength;
            var deltaFiles = new List<string>();

            //First process any files that didn't change, don't even count them in the progress as it will be quick af
            foreach (var maybeDeltaFile in sameFiles)
            {
                UpdateProgress();
                
                Logger.Debug("Processing possible delta file {0}", maybeDeltaFile);
                var newFileLocation = Path.Combine(newVersionLocation, maybeDeltaFile);

                //See if we got a delta file, if so then we process it after
                if (IsDeltaFile(
                    Path.Combine(baseVersionLocation, maybeDeltaFile),
                    newFileLocation))
                {
                    sameFilesLength--;
                    deltaFilesLength++;
                    deltaFiles.Add(maybeDeltaFile);
                    continue;
                }

                //Add a pointer to the file that hasn't changed
                if (await AddSameFile(zipArchive, maybeDeltaFile))
                {
                    fileCount++;
                    continue;
                }
                Logger.Warning("We wasn't able to add {0} that is the same, going to try add it as a \"new\" file", maybeDeltaFile);

                //We wasn't able to add the file as a pointer, try to add it as a new file
                var fileStream = File.OpenRead(Path.Combine(newVersionLocation, newFileLocation));
                if (!await AddNewFile(zipArchive, fileStream, maybeDeltaFile))
                {
                    //Hard bail if we can't even do that
                    Logger.Error("Wasn't able to process file as a new file as well, bailing");
                    Cleanup();
                    return false;
                }
                fileCount++;
            }
            
            //Now process files that was added into the new version
            Logger.Information("Processing files that only exist in the new version");
            foreach (var newFile in newFiles)
            {
                fileCount++;
                Logger.Debug("Processing new file {0}", newFile);

                //Process new file
                var fileStream = File.OpenRead(Path.Combine(newVersionLocation, newFile));
                if (await AddNewFile(zipArchive, fileStream, newFile))
                {
                    fileStream.Dispose();
                    UpdateProgress();
                    continue;
                }

                //if we can't add it then hard fail, can't do anything to save this
                Logger.Error("Wasn't able to process new file, bailing");
                Cleanup();
                return false;
            }

            var result = Parallel.ForEach(deltaFiles, new ParallelOptions { MaxDegreeOfParallelism = concurrentDeltaCreation }, async (deltaFile, state) =>
            {
                var deltaFileLocation = Path.Combine(newVersionLocation, deltaFile);
                Logger.Debug("Processing changed file {0}", deltaFile);

                //Try to add the file as a delta file
                if (await AddDeltaFile(zipArchive,
                    Path.Combine(baseVersionLocation, deltaFile),
                    deltaFileLocation, UpdateProgress))
                {
                    fileCount++;
                    UpdateProgress();
                    return;
                }

                //If we can't make the file as a delta file try to create it as a "new" file
                Logger.Warning("Wasn't able to make delta file, creating file as \"new\" file");
                var fileStream = File.OpenRead(Path.Combine(newVersionLocation, deltaFileLocation));
                if (await AddNewFile(zipArchive, fileStream, deltaFile))
                {
                    fileCount++;
                    UpdateProgress();
                    return;
                }

                //Hard bail if we can't even do that
                Logger.Error("Wasn't able to process file as a new file as well, bailing");
                Cleanup();
                state.Break();
            });
            if (!result.IsCompleted)
            {
                return false;
            }
            
            //We have created the delta file if we get here, do cleanup and then report as success!
            Logger.Information("We are done with creating delta file, cleaning up");
            Cleanup();
            return true;
        }

        private static async Task Wait(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(-1, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                Logger.Debug("Task has been canceled, finished waiting");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
        
        /// <inheritdoc cref="IUpdateCreator.CreateFullPackage"/>
        public async Task<bool> CreateFullPackage(string applicationLocation, string? fullUpdateLocation = null, Action<decimal>? progress = null)
        {
            if (!Directory.Exists(applicationLocation))
            {
                Logger.Error("{0} doesn't exist, can't create update", applicationLocation);
                return false;
            }

            Logger.Debug("Creating full update file");
            var fileCount = 0m;
            var zipArchive = CreateZipArchive(fullUpdateLocation);
            
            var files = 
                Directory.EnumerateFiles(applicationLocation, "*", SearchOption.AllDirectories)
                    .RemovePath(applicationLocation).ToArray();

            foreach (var file in files)
            {
                var fileLocation = Path.Combine(applicationLocation, file);
                
                //We will process the file as a "new" file as we always want to copy it over
                var fileStream = File.OpenRead(fileLocation);
                if (await AddNewFile(zipArchive, fileStream, file))
                {
                    fileCount++;
                    progress?.Invoke(fileCount / (files.LongLength + 1));
                    continue;
                }
                
                //if we can't add it then hard fail, can't do anything to save this
                Logger.Error("Wasn't able to process new file, bailing");
                zipArchive.Dispose();
                progress?.Invoke(1);
                return false;
            }

            zipArchive.Dispose();
            progress?.Invoke(1);
            return true;
        }

        /// <summary>
        /// Creates a <see cref="ZipArchive"/> to store the update
        /// </summary>
        /// <returns></returns>
        private ZipArchive CreateZipArchive(string? updateFileLocation = null)
        {
            //Create the Temp folder in case it doesn't exist
            Directory.CreateDirectory(Global.TempFolder);
            
            //Create the delta file that will contain all our data
            updateFileLocation ??= Path.Combine(Global.TempFolder, Path.GetRandomFileName() + Extension);
            if (File.Exists(updateFileLocation))
            {
                File.Delete(updateFileLocation);
            }
            
            var updateFileStream = File.OpenWrite(updateFileLocation);
            return new ZipArchive(updateFileStream, ZipArchiveMode.Create);
        }
        
        private static bool IsDeltaFile(string baseFileLocation, string newFileLocation)
        {
            //Get the two files from disk
            var baseFileStream = File.OpenRead(baseFileLocation);
            var newFileStream = File.OpenRead(newFileLocation);

            //See if the filesize or hash is different, if so then the file has changed
            var hasChanged = baseFileStream.Length != newFileStream.Length ||
                             SHA256Util.CreateSHA256Hash(baseFileStream) != SHA256Util.CreateSHA256Hash(newFileStream);

            //Dispose streams and then make delta file if file changed
            baseFileStream.Dispose();
            newFileStream.Dispose();

            return hasChanged;
        }
        
        /// <summary>
        /// Creates a delta file and then adds it the <see cref="ZipArchive"/>
        /// </summary>
        /// <param name="zipArchive"><see cref="ZipArchive"/> to add the file too</param>
        /// <param name="baseFileLocation">Old file</param>
        /// <param name="newFileLocation">New file</param>
        /// <param name="progress">Progress of creating delta file (If possible)</param>
        /// <returns>If we was able to create the delta file</returns>
        private static async Task<bool> AddDeltaFile(ZipArchive zipArchive, string baseFileLocation, string newFileLocation, Action<decimal>? progress = null)
        {
            //Create where the delta file can be stored to grab once made 
            var tmpDeltaFile = Path.Combine(Global.TempFolder, Path.GetRandomFileName());

            //Try to create diff file, outputting extension (and maybe a stream) based on what was used to make it
            if (!DeltaCreation.CreateDeltaFile(baseFileLocation, newFileLocation, tmpDeltaFile, out var extension, out var deltaFileStream))
            {
                //Wasn't able to, report back as fail
                Logger.Error("Wasn't able to create delta file");
                if (File.Exists(tmpDeltaFile))
                {
                    File.Delete(tmpDeltaFile);
                }
                return false;
            }

            //Check that we got something to work with
            if (deltaFileStream == null && !File.Exists(tmpDeltaFile))
            {
                Logger.Error("We have no delta file/stream to work off somehow");
                return false;
            }

            //Get hash and filesize to add to file
            var newFileStream = File.OpenRead(newFileLocation);
            var newFilesize = newFileStream.Length;
            var hash = SHA256Util.CreateSHA256Hash(newFileStream);
            newFileStream.Dispose();
            
            
            //Grab file and add it to the set of files
            deltaFileStream ??= File.OpenRead(tmpDeltaFile);
            var addSuccessful = await AddFile(
                zipArchive, 
                deltaFileStream,
                baseFileLocation.GetRelativePath(newFileLocation) + extension, 
                filesize: newFilesize, 
                sha256Hash: hash);
                
            //Dispose stream and report back if we was able to add file
            deltaFileStream.Dispose();
            File.Delete(tmpDeltaFile);
            return addSuccessful;
        }

        /// <summary>
        /// Adds a file that is not in the last version into the <see cref="ZipArchive"/>
        /// </summary>
        /// <param name="zipArchive"><see cref="ZipArchive"/> to add the file too</param>
        /// <param name="fileStream">Files stream to add</param>
        /// <param name="filepath">File to add</param>
        /// <returns>If we was able to add the file</returns>
        private static async Task<bool> AddNewFile(ZipArchive zipArchive, Stream fileStream, string filepath) =>
            await AddFile(zipArchive, fileStream, filepath + ".new", false);
        
        /// <summary>
        /// Adds a file that is the same as the last version into the <see cref="ZipArchive"/>
        /// </summary>
        /// <param name="zipArchive"><see cref="ZipArchive"/> to add the file too</param>
        /// <param name="filepath">File to add</param>
        /// <returns>If we was able to add the file</returns>
        private static async Task<bool> AddSameFile(ZipArchive zipArchive, string filepath) =>
            await AddFile(zipArchive, Stream.Null, filepath + ".diff");

        private static List<CancellationTokenSource> _guids = new();
        
        /// <summary>
        /// Adds the file to the <see cref="ZipArchive"/> with all the needed information
        /// </summary>
        /// <param name="zipArchive"><see cref="ZipArchive"/> to add the file too</param>
        /// <param name="fileStream">Stream of the file contents</param>
        /// <param name="filepath">Path of the file</param>
        /// <param name="keepFileStreamOpen">If we should keep <see cref="fileStream"/> open once done with it</param>
        /// <param name="filesize">The size that the final file should be</param>
        /// <param name="sha256Hash">The hash that the final file should be</param>
        private static async Task<bool> AddFile(
            ZipArchive zipArchive, 
            Stream fileStream, 
            string filepath, 
            bool keepFileStreamOpen = true, 
            long? filesize = null, 
            string? sha256Hash = null)
        {
            var token = new CancellationTokenSource();
            _guids.Add(token);
            if (_guids.Count > 1)
            {
                await Wait(token.Token);
            }
            
            filesize ??= fileStream.Length;
            //Create and add the file contents to the zip
            var zipFileStream = zipArchive.CreateEntry(filepath).Open();
            await fileStream.CopyToAsync(zipFileStream);
            zipFileStream.Dispose();
            
            //If we didn't have any content in the stream then no need to make shasum file
            if (filesize == 0)
            {
                //Dispose fileStream if we been asked to
                if (!keepFileStreamOpen)
                {
                    fileStream.Dispose();
                }
                _guids.Remove(token);
                _guids.FirstOrDefault()?.Cancel();
                return true;
            }

            sha256Hash ??= SHA256Util.CreateSHA256Hash(fileStream);
            //Create .shasum file if we have some content from the fileStream
            var zipShasumStream = zipArchive.CreateEntry(Path.ChangeExtension(filepath, ".shasum")).Open();
            var textWriter = new StreamWriter(zipShasumStream);

            await textWriter.WriteAsync($"{sha256Hash} {filesize}");
            textWriter.Dispose();
            zipShasumStream.Dispose();

            //Dispose fileStream if we been asked to
            if (!keepFileStreamOpen)
            {
                fileStream.Dispose();
            }
            _guids.Remove(token);
            _guids.FirstOrDefault()?.Cancel();
            return true;
        }
    }
}