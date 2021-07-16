using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TinyUpdate.Binary.Delta;
using TinyUpdate.Binary.Extensions;
using TinyUpdate.Core;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Update;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Binary
{
    /// <summary>
    /// Creates update files in a binary format 
    /// </summary>
    public class BinaryCreator : IUpdateCreator
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(BinaryCreator));

        /// <inheritdoc cref="IUpdateCreator.Extension"/>
        public string Extension => ".tuup";

        /// <inheritdoc cref="IUpdateCreator.CreateDeltaPackage"/>
        public async Task<bool> CreateDeltaPackage(
            string newVersionLocation,
            Version newVersion,
            string baseVersionLocation,
            string? deltaUpdateLocation = null,
            int concurrentDeltaCreation = 1,
            OSPlatform? intendedOs = null,
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
                if (deltaFilesLength + newFilesLength == 0 ||
                    fileCount == 0)
                {
                    return;
                }

                //We need that extra 1 so we are at 99% when done (we got some cleanup to do after)
                var progressValue = (fileCount - sameFilesLength + extraProgress + 1) /
                                    (deltaFilesLength + newFilesLength + 2);
                if (progressValue != lastProgress)
                {
                    progress?.Invoke(progressValue);
                    lastProgress = progressValue;
                }
            }

            if (!Directory.Exists(newVersionLocation) ||
                !Directory.Exists(baseVersionLocation))
            {
                Logger.Error("One of the folders don't exist, can't create delta update....");
                return false;
            }

            Logger.Debug("Creating delta file");
            var zipArchive = CreateZipArchive(deltaUpdateLocation);

            void Cleanup()
            {
                lock (zipArchive)
                {
                    zipArchive.Dispose();
                }
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
                Logger.Debug("Processing possible delta file {0}", maybeDeltaFile);
                var newFileLocation = Path.Combine(newVersionLocation, maybeDeltaFile);

                /*See if we got a delta file, if so then store it for
                 processing after files that haven't changed*/
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
                if (AddSameFile(zipArchive, maybeDeltaFile))
                {
                    fileCount++;
                    continue;
                }

                Logger.Warning(
                    "We wasn't able to add {0} as a file that hasn't changed, going to try add it as a \"new\" file",
                    maybeDeltaFile);

                //We wasn't able to add the file as a pointer, try to add it as a new file
                var fileStream = File.OpenRead(Path.Combine(newVersionLocation, newFileLocation));
                if (!AddNewFile(zipArchive, fileStream, maybeDeltaFile))
                {
                    //Hard bail if we can't even do that
                    Logger.Error("Wasn't able to process {0} as a new file as well, bailing", maybeDeltaFile);
                    Cleanup();
                    return false;
                }

                fileCount++;
            }

            //Now process files that was added into the new version
            Logger.Information("Processing files that only exist in the new version");
            foreach (var newFile in newFiles)
            {
                Logger.Debug("Processing new file {0}", newFile);

                //Process new file
                var fileStream = File.OpenRead(Path.Combine(newVersionLocation, newFile));
                if (AddNewFile(zipArchive, fileStream, newFile))
                {
                    fileStream.Dispose();
                    fileCount++;
                    UpdateProgress();
                    continue;
                }

                //if we can't add it then hard fail, can't do anything to save this
                Logger.Error("Wasn't able to process new file, bailing");
                Cleanup();
                return false;
            }

            //Now process files that changed
            var result = Parallel.ForEach(deltaFiles,
                new ParallelOptions {MaxDegreeOfParallelism = concurrentDeltaCreation}, 
                (deltaFile, state) =>
                {
                    var deltaFileLocation = Path.Combine(newVersionLocation, deltaFile);
                    Logger.Debug("Processing changed file {0}", deltaFile);

                    //Try to add the file as a delta file
                    if (AddDeltaFile(zipArchive,
                        Path.Combine(baseVersionLocation, deltaFile),
                        deltaFileLocation, intendedOs, UpdateProgress))
                    {
                        Interlocked.Increment(ref fileCount);
                        UpdateProgress();
                        return;
                    }

                    //If we can't make the file as a delta file try to create it as a "new" file
                    Logger.Warning("Wasn't able to make delta file, creating file as \"new\" file");
                    var fileStream = File.OpenRead(Path.Combine(newVersionLocation, deltaFileLocation));
                    if (AddNewFile(zipArchive, fileStream, deltaFile))
                    {
                        Interlocked.Increment(ref fileCount);
                        UpdateProgress();
                        return;
                    }

                    //Hard bail if we can't even do that
                    Logger.Error("Wasn't able to process file as a new file, bailing");
                    Cleanup();
                    state.Break();
                });
            //This will return false if something failed
            if (!result.IsCompleted)
            {
                return false;
            }
            
            //Add the loader into the package
            if (!AddLoaderFile(zipArchive, newVersion, newVersionLocation))
            {
                Logger.Error("Wasn't able to create loader for this application");
                Cleanup();
                return false;
            }
            fileCount++;
            UpdateProgress();

            //We have created the delta file if we get here, do cleanup and then report as success!
            Logger.Information("We are done with creating the delta file, cleaning up");
            Cleanup();
            return true;
        }

        /// <inheritdoc cref="IUpdateCreator.CreateFullPackage"/>
        public async Task<bool> CreateFullPackage(
            string applicationLocation, 
            Version version,
            string? fullUpdateLocation = null,
            Action<decimal>? progress = null)
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
                if (AddNewFile(zipArchive, fileStream, file))
                {
                    fileCount++;
                    progress?.Invoke(fileCount / (files.LongLength + 2));
                    continue;
                }

                //if we can't add it then hard fail, can't do anything to save this
                Logger.Error("Wasn't able to process file, bailing");
                zipArchive.Dispose();
                progress?.Invoke(1);
                return false;
            }

            //Add the loader into the package
            if (!AddLoaderFile(zipArchive, version, applicationLocation))
            {
                Logger.Error("Wasn't able to create loader for this application");
                zipArchive.Dispose();
                progress?.Invoke(1);
                return false;
            }
            fileCount++;
            progress?.Invoke(fileCount / (files.LongLength + 2));

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
            //Create the delta file that will contain all our data
            Directory.CreateDirectory(Global.TempFolder);
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

        private static bool AddLoaderFile(
            ZipArchive zipArchive, 
            Version version, 
            string applicationLocation)
        {
            Directory.CreateDirectory(Global.TempFolder);

            //TODO: Grab metadata from .exe and drop it into Loader
            var iconLocation = Path.Combine(applicationLocation, "app.ico");
            var successful = ApplicationLoaderCreator.CreateLoader(
                $"app-{version.ToString(4)}\\{Global.ApplicationName}.exe",
                File.Exists(iconLocation) ? iconLocation : null,
                Global.TempFolder,
                Global.ApplicationName);
            if (successful 
                && AddFile(zipArchive, 
                    File.OpenRead(Path.Combine(Global.TempFolder, Global.ApplicationName + ".exe")),
                    Global.ApplicationName + ".exe.load", false))
            {
                return true;
            }
            Logger.Error("Wasn't able to add loader file to list of files");
            return false;
        }

        /// <summary>
        /// Creates a delta file and then adds it the <see cref="ZipArchive"/>
        /// </summary>
        /// <param name="zipArchive"><see cref="ZipArchive"/> to add the file too</param>
        /// <param name="baseFileLocation">Old file</param>
        /// <param name="newFileLocation">New file</param>
        /// <param name="intendedOS">What OS this delta file will be intended for</param>
        /// <param name="progress">Progress of creating delta file (If possible)</param>
        /// <returns>If we was able to create the delta file</returns>
        private static bool AddDeltaFile(ZipArchive zipArchive, string baseFileLocation,
            string newFileLocation, OSPlatform? intendedOS, Action<decimal>? progress = null)
        {
            //Create where the delta file can be stored to grab once made
            Directory.CreateDirectory(Global.TempFolder);
            var tmpDeltaFile = Path.Combine(Global.TempFolder, Path.GetRandomFileName());

            //Try to create diff file, outputting extension (and maybe a stream) based on what was used to make it
            if (!DeltaCreation.CreateDeltaFile(baseFileLocation, newFileLocation, tmpDeltaFile, intendedOS,
                out var extension, out var deltaFileStream))
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
            var addSuccessful = AddFile(
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
        private static bool AddNewFile(ZipArchive zipArchive, Stream fileStream, string filepath) =>
            AddFile(zipArchive, fileStream, filepath + ".new", false);

        /// <summary>
        /// Adds a file that is the same as the last version into the <see cref="ZipArchive"/>
        /// </summary>
        /// <param name="zipArchive"><see cref="ZipArchive"/> to add the file too</param>
        /// <param name="filepath">File to add</param>
        /// <returns>If we was able to add the file</returns>
        private static bool AddSameFile(ZipArchive zipArchive, string filepath) =>
            AddFile(zipArchive, Stream.Null, filepath + ".diff");

        /// <summary>
        /// Adds the file to the <see cref="ZipArchive"/> with all the needed information
        /// </summary>
        /// <param name="zipArchive"><see cref="ZipArchive"/> to add the file too</param>
        /// <param name="fileStream">Stream of the file contents</param>
        /// <param name="filepath">Path of the file</param>
        /// <param name="keepFileStreamOpen">If we should keep <see cref="fileStream"/> open once done with it</param>
        /// <param name="filesize">The size that the final file should be</param>
        /// <param name="sha256Hash">The hash that the final file should be</param>
        private static bool AddFile(
            ZipArchive zipArchive,
            Stream fileStream,
            string filepath,
            bool keepFileStreamOpen = true,
            long? filesize = null,
            string? sha256Hash = null)
        {
            //Create token and then wait if something else is currently adding a file
            filesize ??= fileStream.Length;

            //Create and add the file contents to the zip
            lock (zipArchive)
            {
                var zipFileStream = zipArchive.CreateEntry(filepath).Open();
                fileStream.CopyTo(zipFileStream);
                zipFileStream.Dispose();
                    
                //If we didn't have any content in the stream then no need to make shasum file
                if (filesize == 0)
                {
                    //Dispose fileStream if we been asked to
                    if (!keepFileStreamOpen)
                    {
                        fileStream.Dispose();
                    }

                    return true;
                }
                    
                //Create .shasum file if we have some content from the fileStream
                sha256Hash ??= SHA256Util.CreateSHA256Hash(fileStream);
                    
                using var zipShasumStream = zipArchive.CreateEntry(Path.ChangeExtension(filepath, ".shasum")).Open();
                using var textWriter = new StreamWriter(zipShasumStream);
                textWriter.Write($"{sha256Hash} {filesize}");
            }

            //Dispose fileStream if we been asked to
            if (!keepFileStreamOpen)
            {
                fileStream.Dispose();
            }

            return true;
        }
    }
}