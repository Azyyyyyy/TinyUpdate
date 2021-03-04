using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using DeltaCompressionDotNet.MsDelta;
using TinyUpdate.Core;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Update;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Binary
{
    //TODO: Check OS being ran in MSDiff
    //TODO: Add a DeltaCreation class for allowing multiple deltas to be made at the same time (Warn about CPU + Mem with this)
    /// <summary>
    /// Creates update files in a binary format 
    /// </summary>
    public class BinaryCreator : IUpdateCreator
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger("BinaryCreator");

        /// <inheritdoc cref="IUpdateCreator.CreateDeltaPackage"/>
        public async Task<bool> CreateDeltaPackage(string newVersionLocation, string baseVersionLocation, Action<decimal>? progress = null)
        {
            //To keep track of progress
            long fileCount = 0;
            long sameFilesLength;
            long newFilesLength;
            decimal lastProgress = 0;
            
            void UpdateProgress(decimal extraProgress = 0)
            {
                //We need that extra 1 so we are at 99% when done (we got some cleanup to do after)
                var progressValue = (fileCount + extraProgress) / (sameFilesLength + newFilesLength + 1);
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
            
            var zipArchive = CreateZipArchive();

            void Cleanup()
            {
                zipArchive.Dispose();
                progress?.Invoke(1);
            }
            
            //Get all the files that are in the new version (Removing the Path so we only have the relative path of the file)
            var newVersionFiles = RemovePath(
                Directory.EnumerateFiles(newVersionLocation, "*", SearchOption.AllDirectories), 
                newVersionLocation).ToArray();
            
            //and get the files from the old version
            var baseVersionFiles = RemovePath(
                Directory.EnumerateFiles(baseVersionLocation, "*", SearchOption.AllDirectories), 
                baseVersionLocation).ToArray();

            //Find any files that are in both version and process them based on if they had any changes
            Logger.Information("Processing files that are in both versions");
            var sameFiles = newVersionFiles.Where(x => baseVersionFiles.Contains(x)).ToArray();
            var newFiles = newVersionFiles.Where(x => !sameFiles.Contains(x)).ToArray();

            sameFilesLength = sameFiles.LongLength;
            newFilesLength = newFiles.LongLength;
            
            foreach (var maybeDeltaFile in sameFiles)
            {
                Logger.Debug("Processing possible delta file {0}", maybeDeltaFile);
                var newFileLocation = Path.Combine(newVersionLocation, maybeDeltaFile);

                //Try to create the file as a delta file
                if (await ProcessMaybeDeltaFile(zipArchive,
                    Path.Combine(baseVersionLocation, maybeDeltaFile),
                    newFileLocation, UpdateProgress))
                {
                    fileCount++;
                    UpdateProgress();
                    continue;
                }

                //If we can't make the file as a delta file try to create it as a "new" file
                Logger.Warning("Wasn't able to make delta file, creating file as \"new\" file");
                var fileStream = File.OpenRead(Path.Combine(newVersionLocation, newFileLocation));
                if (await AddNewFile(zipArchive, fileStream, maybeDeltaFile))
                {
                    fileCount++;
                    UpdateProgress();
                    continue;
                }

                //Hard bail if we can't even do that
                Logger.Error("Wasn't able to process file as a new file as well, bailing");
                Cleanup();
                return false;
            }

            //Process files that was added into the new version
            Logger.Information("Processing files that only exist in the new version");
            foreach (var newFile in newFiles)
            {
                fileCount++;
                Logger.Debug("Processing new file {0}", newFile);

                //Process file
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

            //We have created the delta file if we get here, do cleanup and then report as success!
            Logger.Information("We are done with creating delta file, cleaning up");
            Cleanup();
            return true;
        }

        /// <inheritdoc cref="IUpdateCreator.CreateFullPackage"/>
        public async Task<bool> CreateFullPackage(string applicationLocation, Action<decimal>? progress = null)
        {
            if (!Directory.Exists(applicationLocation))
            {
                Logger.Error("{0} doesn't exist, can't create update", applicationLocation);
                return false;
            }

            var fileCount = 0m;
            var zipArchive = CreateZipArchive();
            
            var files = RemovePath(
                Directory.EnumerateFiles(applicationLocation, "*", SearchOption.AllDirectories), 
                applicationLocation).ToArray();

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
                progress?.Invoke(100);
                return false;
            }

            zipArchive.Dispose();
            progress?.Invoke(100);
            return true;
        }

        /// <summary>
        /// Creates a <see cref="ZipArchive"/> to store the update
        /// </summary>
        /// <returns></returns>
        private static ZipArchive CreateZipArchive()
        {
            //Create the Temp folder in case it doesn't exist
            Directory.CreateDirectory(Global.TempFolder);
            
            //Create the delta file that will contain all our data
            Logger.Debug("Creating delta file");
            var deltaFileLocation = Path.Combine(Global.TempFolder, Path.GetRandomFileName() + Global.TinyUpdateExtension);
            var deltaFileStream = File.OpenWrite(deltaFileLocation);
            return new ZipArchive(deltaFileStream, ZipArchiveMode.Create);
        }

        /// <summary>
        /// Checks a file that might be a delta file and passes it to the correct functions
        /// </summary>
        /// <param name="zipArchive">zipArchive to add file to</param>
        /// <param name="baseFileLocation">File that matches the new file</param>
        /// <param name="newFileLocation">New file to compare</param>
        /// <param name="progress">Progress of creating delta file (if file is a delta file)</param>
        /// <returns>If we was able to create the file needed</returns>
        private static async Task<bool> ProcessMaybeDeltaFile(ZipArchive zipArchive, string baseFileLocation, string newFileLocation, Action<decimal>? progress = null)
        {
            //Get the two files from disk
            var baseFileStream = File.OpenRead(baseFileLocation);
            var newFileStream = File.OpenRead(newFileLocation);

            //See if the filesize or hash is different, if so then the file has changed
            var hasChanged = baseFileStream.Length != newFileStream.Length ||
                             SHA1Util.CreateSHA1Hash(baseFileStream) != SHA1Util.CreateSHA1Hash(newFileStream);

            //Dispose streams and then make delta file if file changed
            baseFileStream.Dispose();
            newFileStream.Dispose();
            if (hasChanged)
            {
                Logger.Information("Making Delta file");
                return await AddDeltaFile(zipArchive, baseFileLocation, newFileLocation, progress);
            }

            //If both checks return as true then make something that the Applier can point back to
            Logger.Information("Making same file pointer");
            return await AddSameFile(zipArchive, GetRelativePath(baseFileLocation, newFileLocation));
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
            Stream? deltaFileStream = null;
            
            //Try to create diff file, outputting extension (and maybe a stream) based on what was used to make it
            if (!CreateMSDiffFile(baseFileLocation, newFileLocation, tmpDeltaFile, out var extension) &&
                !CreateBSDiffFile(baseFileLocation, newFileLocation, tmpDeltaFile, out extension, out deltaFileStream, progress))
            {
                //Wasn't able to, report back as fail
                Logger.Error("Wasn't able to create delta file");
                return false;
            }

            if (deltaFileStream == null && !File.Exists(tmpDeltaFile))
            {
                Logger.Error("We have no delta file/stream to work off somehow");
                return false;
            }

            //Get hash and filesize to add to file
            var newFileStream = File.OpenRead(newFileLocation);
            var newFilesize = newFileStream.Length;
            var hash = SHA1Util.CreateSHA1Hash(newFileStream);
            newFileStream.Dispose();
            
            //Grab file and add it to the set of files
            deltaFileStream ??= File.OpenRead(tmpDeltaFile);
            var addSuccessful = await AddFile(zipArchive, deltaFileStream,
                GetRelativePath(baseFileLocation, newFileLocation) + extension, filesize: newFilesize, sha1Hash: hash);
                
            //Dispose stream and report back if we was able to add file
            deltaFileStream.Dispose();
            return addSuccessful;
        }

        /// <summary>
        /// Creates a delta file using <see cref="BinaryPatchUtility.Create"/>
        /// </summary>
        /// <param name="baseFileLocation">Old file location</param>
        /// <param name="newFileLocation">New file location</param>
        /// <param name="deltaFileLocation">Where to output the delta file</param>
        /// <param name="extension">What extension to know it was made using this when applying the delta</param>
        /// <param name="deltaFileStream">Stream with the </param>
        /// <param name="progress">Reports back progress</param>
        /// <returns>If we was able to create the delta file</returns>
        private static bool CreateBSDiffFile(
            string baseFileLocation, 
            string newFileLocation, 
            string deltaFileLocation, 
            out string extension, 
            out Stream? deltaFileStream, 
            Action<decimal>? progress = null)
        {
            extension = ".bsdiff";
            deltaFileStream = new MemoryStream();

            var success = BinaryPatchUtility.Create(GetBytesFromFile(baseFileLocation),
                    GetBytesFromFile(newFileLocation), deltaFileStream, progress);

            if (!success)
            {
                deltaFileStream.Dispose();
                deltaFileStream = null;
            }

            deltaFileStream?.Seek(0, SeekOrigin.Begin);
            return success;
        }

        /// <summary>
        /// Creates a delta file using <see cref="BinaryPatchUtility.Create"/>
        /// </summary>
        /// <param name="baseFileLocation">Old file location</param>
        /// <param name="newFileLocation">New file location</param>
        /// <param name="deltaFileLocation">Where to output the delta file</param>
        /// <param name="extension">What extension to know it was made using this when applying the delta</param>
        /// <returns>If we was able to create the delta file</returns>
        private static bool CreateMSDiffFile(string baseFileLocation, string newFileLocation, string deltaFileLocation, out string extension)
        {
            extension = ".diff";
            var msDelta = new MsDeltaCompression();
            try
            {
                msDelta.CreateDelta(baseFileLocation, newFileLocation, deltaFileLocation);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Gets the contents of a file in a <see cref="byte"/>[]
        /// </summary>
        /// <param name="fileLocation">Where the file is located</param>
        /// <returns>The files contents</returns>
        private static byte[] GetBytesFromFile(string fileLocation)
        {
            //Get stream and then make byte[] to fill
            var stream = File.OpenRead(fileLocation);

            //Fill byte[] and dispose stream
            var data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);
            stream.Dispose();

            //Return contents
            return data;
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

        /// <summary>
        /// Adds the file to the <see cref="ZipArchive"/> with all the needed information
        /// </summary>
        /// <param name="zipArchive"><see cref="ZipArchive"/> to add the file too</param>
        /// <param name="fileStream">Stream of the file contents</param>
        /// <param name="filepath">Path of the file</param>
        /// <param name="keepFileStreamOpen">If we should keep <see cref="fileStream"/> open once done with it</param>
        /// <param name="filesize">The size that the final file should be</param>
        /// <param name="sha1Hash">The hash that the final file should be</param>
        private static async Task<bool> AddFile(
            ZipArchive zipArchive, 
            Stream fileStream, 
            string filepath, 
            bool keepFileStreamOpen = true, 
            long? filesize = null, 
            string? sha1Hash = null)
        {
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
                return true;
            }

            sha1Hash ??= SHA1Util.CreateSHA1Hash(fileStream);
            //Create .shasum file if we have some content from the fileStream
            var zipShasumStream = zipArchive.CreateEntry(Path.ChangeExtension(filepath, ".shasum")).Open();
            var textWriter = new StreamWriter(zipShasumStream);

            await textWriter.WriteAsync($"{sha1Hash} {filesize}");
            textWriter.Dispose();
            zipShasumStream.Dispose();

            //Dispose fileStream if we been asked to
            if (!keepFileStreamOpen)
            {
                fileStream.Dispose();
            }
            return true;
        }
        
        /// <summary>
        /// Removes path from string
        /// </summary>
        /// <param name="enumerable">file paths that contain the path</param>
        /// <param name="path">Path to remove</param>
        /// <returns>file paths without <see cref="path"/></returns>
        private static IEnumerable<string> RemovePath(IEnumerable<string> enumerable, string path)
        {
            return enumerable.Select(file => 
                file.Remove(0, path.Length + 1));
        }
        
        //TODO: Make as extension so when on std 2.1 we can safely #if this out
        private static string GetRelativePath(string baseFile, string newFile)
        {
            //If we get here then this is the same here
            var basePath = baseFile;
            var newPath = newFile;
            while (!newPath.Contains(basePath))
            {
                basePath = basePath.Remove(0, basePath.IndexOf(Path.DirectorySeparatorChar) + 1);
            }

            newPath = newFile.Replace(basePath, "");
            newPath = newFile.Remove(0, newPath.Length);
            return newPath;
        }
    }
}