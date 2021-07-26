using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
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
    /// <summary>
    /// Creates update files in a binary format 
    /// </summary>
    public class BinaryCreator : IUpdateCreator
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(BinaryCreator));

        /// <inheritdoc cref="IUpdateCreator.Extension"/>
        public string Extension => ".tuup";

        /// <inheritdoc cref="IUpdateCreator.CreateDeltaPackage"/>
        [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
        public bool CreateDeltaPackage(
            ApplicationMetadata applicationMetadata,
            string newVersionLocation,
            Version newVersion,
            string baseVersionLocation,
            Version oldVersion,
            string outputFolder,
            string? deltaUpdateLocation = null,
            OSPlatform? intendedOs = null,
            Action<double>? progress = null)
        {
            //To keep track of progress
            long fileCount = 0;
            long deltaFilesLength = 0;
            long newFilesLength;
            long sameFilesLength;

            double lastProgress = 0;

            void UpdateProgress(double extraProgress = 0)
            {
                if (deltaFilesLength + newFilesLength == 0 
                    || fileCount == 0)
                {
                    return;
                }

                //We need that extra 1 so we are at 99% when done (we got some cleanup to do after)
                var progressValue = 
                    (fileCount - sameFilesLength + extraProgress + 1) / (deltaFilesLength + newFilesLength + 2);
                if (progressValue != lastProgress)
                {
                    progress?.Invoke(progressValue);
                    lastProgress = progressValue;
                }
            }

            if (!Directory.Exists(newVersionLocation) 
                || !Directory.Exists(baseVersionLocation))
            {
                Logger.Error("One of the folders don't exist, can't create delta update....");
                return false;
            }

            Logger.Debug("Creating delta file");
            var zipArchive = CreateZipArchive(applicationMetadata.TempFolder, deltaUpdateLocation);

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
            var sameFiles = newVersionFiles.Where(x => baseVersionFiles.Contains(x)).ToArray();
            var newFiles = newVersionFiles.Where(x => !sameFiles.Contains(x)).ToArray();

            newFilesLength = newFiles.LongLength;
            sameFilesLength = sameFiles.LongLength;
            var deltaFiles = new List<string>(sameFiles.Length);

            //First process any files that didn't change, don't even count them in the progress as it will be quick af
            Logger.Information("Processing files that are in both versions");
            foreach (var maybeDeltaFile in sameFiles)
            {
                Logger.Debug("Processing possible delta file {0}", maybeDeltaFile);
                var newFileLocation = Path.Combine(newVersionLocation, maybeDeltaFile);

                /*See if we got a delta file, if so then store it for
                 processing after files that haven't changed*/
                if (IsDeltaFile(Path.Combine(baseVersionLocation, maybeDeltaFile),
                    newFileLocation))
                {
                    sameFilesLength--;
                    deltaFilesLength++;
                    deltaFiles.Add(maybeDeltaFile);
                    continue;
                }

                //Add a pointer to the file that hasn't changed
                Logger.Debug("{0} hasn't changed, processing as unchanged file", maybeDeltaFile);

                var fileStream = File.OpenRead(newFileLocation);
                if (AddSameFile(zipArchive, maybeDeltaFile, SHA256Util.CreateSHA256Hash(fileStream)))
                {
                    fileCount++;
                    fileStream.Dispose();
                    continue;
                }

                Logger.Warning("We wasn't able to add {0} as a file that was unchanged, adding as a \"new\" file",
                    maybeDeltaFile);

                //We wasn't able to add the file as a pointer, try to add it as a new file
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
            var result = Parallel.ForEach(deltaFiles, (deltaFile, state) =>
                {
                    var deltaFileLocation = Path.Combine(newVersionLocation, deltaFile);
                    Logger.Debug("Processing changed file {0}", deltaFile);

                    //Try to add the file as a delta file
                    if (AddDeltaFile(applicationMetadata.TempFolder,
                        zipArchive,
                        Path.Combine(baseVersionLocation, deltaFile),
                        deltaFileLocation, 
                        intendedOs,
                        UpdateProgress))
                    {
                        Interlocked.Increment(ref fileCount);
                        UpdateProgress();
                        return;
                    }

                    //If we can't make the file as a delta file try to create it as a "new" file
                    Logger.Warning("Wasn't able to make delta file, adding file as \"new\" file");
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
            
            if (!AddLoaderFile(
                applicationMetadata, 
                zipArchive, 
                newVersion, 
                newVersionLocation,
                intendedOs,
                oldVersion,
                outputFolder))
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
        public bool CreateFullPackage(
            ApplicationMetadata applicationMetadata,
            string applicationLocation, 
            Version version,
            string? fullUpdateLocation = null,
            Action<double>? progress = null)
        {
            if (!Directory.Exists(applicationLocation))
            {
                Logger.Error("{0} doesn't exist, can't create update", applicationLocation);
                return false;
            }
            Logger.Debug("Creating full update file");

            var zipArchive = CreateZipArchive(applicationMetadata.TempFolder, fullUpdateLocation);
            var files = Directory.EnumerateFiles(applicationLocation, "*", SearchOption.AllDirectories).ToArray();

            var fileCount = 0d;
            foreach (var file in files)
            {
                //We will process the file as a "new" file as we always want to copy it over
                var fileStream = File.OpenRead(file);
                if (AddNewFile(zipArchive, fileStream, file.RemovePath(applicationLocation)))
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
            if (!AddLoaderFile(applicationMetadata, zipArchive, version, applicationLocation))
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
        private ZipArchive CreateZipArchive(string tempFolder, string? updateFileLocation = null)
        {
            //Create the delta file that will contain all our data
            Directory.CreateDirectory(tempFolder);
            updateFileLocation ??= Path.Combine(tempFolder, Path.GetRandomFileName() + Extension);
            if (File.Exists(updateFileLocation))
            {
                Logger.Warning("{0} already exists, deleting...", updateFileLocation);
                File.Delete(updateFileLocation);
            }

            var updateFileStream = File.OpenWrite(updateFileLocation);
            return new ZipArchive(updateFileStream, ZipArchiveMode.Create);
        }

        private static bool IsDeltaFile(string baseFileLocation, string newFileLocation)
        {
            //Get the two files from disk
            using var baseFileStream = File.OpenRead(baseFileLocation);
            using var newFileStream = File.OpenRead(newFileLocation);

            //See if the filesize or hash is different, if so then the file has changed
            var hasChanged = 
                baseFileStream.Length != newFileStream.Length 
                || SHA256Util.CreateSHA256Hash(baseFileStream) != SHA256Util.CreateSHA256Hash(newFileStream);

            return hasChanged;
        }
        
        private bool AddLoaderFile(
            ApplicationMetadata applicationMetadata,
            ZipArchive zipArchive,
            Version newVersion,
            string applicationLocation,
            OSPlatform? intendedOs = null,
            Version? oldVersion = null,
            string? outputLocation = null)
        {
            string loaderLocation = Path.Combine(applicationMetadata.TempFolder, applicationMetadata.ApplicationName + ".exe");
            // ReSharper disable once LocalFunctionHidesMethod
            bool AddFile() => BinaryCreator.AddFile(
                zipArchive,
                File.OpenRead(loaderLocation),
                applicationMetadata.ApplicationName + ".exe.load", 
                false);
            
            Directory.CreateDirectory(applicationMetadata.TempFolder);

            //TODO: Grab metadata from .exe and drop it into Loader
            var iconLocation = Path.Combine(applicationLocation, "app.ico");
            iconLocation = File.Exists(iconLocation) ? iconLocation : null;
            
            var successful = LoaderCreator.CreateLoader(
                applicationMetadata.TempFolder,
                $"{newVersion.GetApplicationFolder()}\\{applicationMetadata.ApplicationName}.exe",
                iconLocation,
                loaderLocation,
                 applicationMetadata.ApplicationName, 
                intendedOs);

            if (successful != LoadCreateStatus.Successful)
            {
                var canContinue = successful == LoadCreateStatus.UnableToCreate;
                Logger.Error("We wasn't able to create loader! (Going to fail file creation?: {0})", canContinue);
                return canContinue;
            }
            if (oldVersion == null || !Directory.Exists(outputLocation))
            {
                return AddFile();
            }

            //If we get here then we might also have the old loader, try to diff by using it
            foreach (var file in Directory.EnumerateFiles(outputLocation, "*" + Extension))
            {
                /*Don't try it with delta file, more then likely going to
                  have diff loader itself and we can't work with that*/
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.EndsWith("-delta")
                    || fileName.ToVersion() != oldVersion)
                {
                    continue;
                }

                Stream? stream = null;
                ZipArchive fileArch;
                try
                {
                    stream = File.OpenRead(file);
                    fileArch = new ZipArchive(stream, ZipArchiveMode.Read);
                }
                catch (Exception e)
                {
                    stream?.Dispose();
                    Logger.Error(e);
                    continue;
                }

                //We want to grab the loader file
                var loaderFileIndex = fileArch.Entries.IndexOf(x => x?.Name == applicationMetadata.ApplicationName + ".exe.load");
                if (loaderFileIndex == -1)
                {
                    fileArch.Dispose();
                    continue;
                }

                var fileEntry = fileArch.Entries[loaderFileIndex];
                var tmpFile = Path.Combine(applicationMetadata.TempFolder, Path.GetFileNameWithoutExtension(fileEntry.Name));
                fileEntry.ExtractToFile(tmpFile, true);

                var deltaSuccessful = AddDeltaFile(applicationMetadata.TempFolder, zipArchive, tmpFile, loaderLocation, extensionEnd: "load");
                File.Delete(tmpFile);
                if (!deltaSuccessful)
                {
                    Logger.Warning("Wasn't able to diff loader, just going to add the load in as normal");
                }
                fileArch.Dispose();
                return deltaSuccessful || AddFile();
            }
            
            //If we get here then we can't do any kind of diff logic
            return AddFile();
        }

        /// <summary>
        /// Creates a delta file and then adds it the <see cref="ZipArchive"/>
        /// </summary>
        /// <param name="tempFolder">Where the temp folder is located</param>
        /// <param name="zipArchive"><see cref="ZipArchive"/> to add the file too</param>
        /// <param name="baseFileLocation">Old file</param>
        /// <param name="newFileLocation">New file</param>
        /// <param name="intendedOs">What OS this delta file will be intended for</param>
        /// <param name="progress">Progress of creating delta file (If possible)</param>
        /// <param name="extensionEnd">What to add onto the end of the extension (If needed)</param>
        /// <returns>If we was able to create the delta file</returns>
        private static bool AddDeltaFile(
            string tempFolder, 
            ZipArchive zipArchive, 
            string baseFileLocation,
            string newFileLocation,
            OSPlatform? intendedOs = null,
            Action<double>? progress = null, 
            string? extensionEnd = null)
        {
            //Create where the delta file can be stored to grab once made
            Directory.CreateDirectory(tempFolder);
            var tmpDeltaFile = Path.Combine(tempFolder, Path.GetRandomFileName());

            //Try to create diff file, outputting extension (and maybe a stream) based on what was used to make it
            if (!DeltaCreation.CreateDeltaFile(
                tempFolder, 
                baseFileLocation, 
                newFileLocation, 
                tmpDeltaFile, 
                intendedOs,
                out var extension, 
                out var deltaFileStream))
            {
                //Wasn't able to create delta file, report back as fail
                Logger.Error("Wasn't able to create delta file");
                File.Delete(tmpDeltaFile);
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
                baseFileLocation.GetRelativePath(newFileLocation) + extension + extensionEnd,
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
        private static bool AddSameFile(ZipArchive zipArchive, string filepath, string hash) =>
            AddFile(zipArchive, Stream.Null, filepath + ".diff", sha256Hash: hash);

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