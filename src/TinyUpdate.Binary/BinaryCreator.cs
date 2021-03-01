using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using DeltaCompressionDotNet.MsDelta;
using TinyUpdate.Core;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Binary
{
    //TODO: Create logging
    //TODO: Report back the progress
    //TODO: Create CreateFullPackage
    /// <summary>
    /// Creates update files in a binary format 
    /// </summary>
    public class BinaryCreator : IUpdateCreator
    {
        public async Task<bool> CreateDeltaPackage(string newVersionLocation, string baseVersionLocation, Action<decimal>? progress = null)
        {
            if (!Directory.Exists(newVersionLocation) || 
                !Directory.Exists(baseVersionLocation))
            {
                Trace.WriteLine("One of the folders don't exist, can't create....");
                return false;
            }
            
            //Create the Temp folder in case it doesn't exist
            Directory.CreateDirectory(Global.TempFolder);

            Trace.WriteLine("Creating delta file");
            //Create the delta file that will contain all our data
            var deltaFileLocation = Path.Combine(Global.TempFolder, Path.GetRandomFileName() + Global.TinyUpdateExtension);
            var deltaFileStream = File.OpenWrite(deltaFileLocation);
            var zipArchive = new ZipArchive(deltaFileStream, ZipArchiveMode.Create);

            void Cleanup()
            {
                zipArchive.Dispose();
                deltaFileStream.Dispose();
            }
            
            //Get all the files that are in the new version (Removing the Path so we only have the relative path of the file)
            var newVersionFiles = RemovePath(
                Directory.EnumerateFiles(newVersionLocation, "*", SearchOption.AllDirectories), 
                newVersionLocation).ToArray();
            
            //and get the files from the old version
            var baseVersionFiles = RemovePath(
                Directory.EnumerateFiles(baseVersionLocation, "*", SearchOption.AllDirectories), 
                baseVersionLocation).ToArray();
            
            Trace.WriteLine("Processing files that are in both versions");
            //Find any files that are in both version and process them based on if they had any changes
            var sameFiles = newVersionFiles.Where(x => baseVersionFiles.Contains(x)).ToArray();
            foreach (var maybeDeltaFile in sameFiles)
            {
                Trace.WriteLine($"Processing {maybeDeltaFile}");
                var newFileLocation = Path.Combine(newVersionLocation, maybeDeltaFile);

                //Try to create the file as a delta file
                if (await ProcessMaybeDeltaFile(zipArchive,
                    Path.Combine(baseVersionLocation, maybeDeltaFile),
                    newFileLocation))
                {
                    continue;
                }

                //If we can't make the file as a delta file try to create it as a "new" file
                Trace.WriteLine("Wasn't able to make delta file, creating file as \"new\" file");
                var fileStream = File.OpenRead(Path.Combine(newVersionLocation, newFileLocation));
                if (await AddNewFile(zipArchive, fileStream, maybeDeltaFile))
                {
                    continue;
                }
                
                //Hard bail if we can't even do that
                Trace.WriteLine("Wasn't able to process file as a new file as well, bailing");
                Cleanup();
                return false;
            }

            //Process files that was added into the new version
            Trace.WriteLine("Processing files that only exist in the new version");
            foreach (var newFile in newVersionFiles.Where(x => !sameFiles.Contains(x)))
            {
                //Process file
                var fileStream = File.OpenRead(Path.Combine(newVersionLocation, newFile));
                if (await AddNewFile(zipArchive, fileStream, newFile))
                {
                    fileStream.Dispose();
                    continue;
                }
                
                //if we can't add it then hard fail, can't do anything to save this
                Trace.WriteLine("Wasn't able to process new file, bailing");
                Cleanup();
                return false;
            }
            
            //We have created the delta file if we get here, do cleanup and then report as success!
            Trace.WriteLine("We are done with creating delta file, cleaning up");
            Cleanup();
            return true;
        }

        public Task<bool> CreateFullPackage(string applicationLocation, Action<decimal>? progress = null)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Checks a file that might be a delta file and passes it to the correct functions
        /// </summary>
        /// <param name="zipArchive">zipArchive to add file to</param>
        /// <param name="baseFileLocation">File that matches the new file</param>
        /// <param name="newFileLocation">New file to compare</param>
        /// <returns>If we was able to create the file needed</returns>
        private static async Task<bool> ProcessMaybeDeltaFile(ZipArchive zipArchive, string baseFileLocation, string newFileLocation)
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
                Trace.WriteLine("Making Delta file");
                return await AddDeltaFile(zipArchive, baseFileLocation, newFileLocation);
            }

            //If both checks return as true then make something that the Applier can point back to
            Trace.WriteLine("Making same file pointer");
            return await AddSameFile(zipArchive, GetRelativePath(baseFileLocation, newFileLocation));
        }

        /// <summary>
        /// Creates a delta file and then adds it the <see cref="ZipArchive"/>
        /// </summary>
        /// <param name="zipArchive"><see cref="ZipArchive"/> to add the file too</param>
        /// <param name="baseFileLocation">Old file</param>
        /// <param name="newFileLocation">New file</param>
        /// <returns>If we was able to create the delta file</returns>
        private static async Task<bool> AddDeltaFile(ZipArchive zipArchive, string baseFileLocation, string newFileLocation)
        {
            //Create where the delta file can be stored to grab once made 
            var tmpDeltaFile = Path.Combine(Global.TempFolder, Path.GetRandomFileName());

            //Try to create diff file, outputting extension based on what was used to make it
            if (!CreateMSDiffFile(baseFileLocation, newFileLocation, tmpDeltaFile, out var extension) &&
                !CreateBSDiffFile(baseFileLocation, newFileLocation, tmpDeltaFile, out extension))
            {
                //Wasn't able to, report back as fail
                return false;
            }

            var newFileStream = File.OpenRead(newFileLocation);
            var newFilesize = newFileStream.Length;
            var hash = SHA1Util.CreateSHA1Hash(newFileStream);
            newFileStream.Dispose();
            
            //Grab file and add it to the set of files
            var deltaFileStream = File.OpenRead(tmpDeltaFile);
            var addSuccessful = await AddFile(zipArchive, deltaFileStream,
                GetRelativePath(baseFileLocation, newFileLocation) + extension, filesize: newFilesize, sha1hash: hash);
                
            //Dispose stream and report back if we was able to add file
            deltaFileStream.Dispose();
            return addSuccessful;
        }

        /// <summary>
        /// Creates a delta file using <see cref="BinaryPatchUtility.Create(byte[], byte[], Stream)"/>
        /// </summary>
        /// <param name="baseFileLocation">Old file location</param>
        /// <param name="newFileLocation">New file location</param>
        /// <param name="deltaFileLocation">Where to output the delta file</param>
        /// <param name="extension">What extension to know it was made using this when applying the delta</param>
        /// <returns>If we was able to create the delta file</returns>
        private static bool CreateBSDiffFile(string baseFileLocation, string newFileLocation, string deltaFileLocation, out string extension)
        {
            //TODO: Don't write this to disk but to memoryStream
            extension = ".bsdiff";
            var outputStream = File.OpenWrite(deltaFileLocation);

            var success = BinaryPatchUtility.Create(GetBytesFromFile(baseFileLocation).ToArray(),
                    GetBytesFromFile(newFileLocation).ToArray(), outputStream);

            outputStream.Dispose();
            return success;
        }

        /// <summary>
        /// Gets the contents of a file in a <see cref="byte"/>[]
        /// </summary>
        /// <param name="fileLocation">Where the file is located</param>
        /// <returns>The files contents</returns>
        private static Span<byte> GetBytesFromFile(string fileLocation)
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
        /// Creates a delta file using <see cref="BinaryPatchUtility.Create(byte[], byte[], Stream)"/>
        /// </summary>
        /// <param name="baseFileLocation">Old file location</param>
        /// <param name="newFileLocation">New file location</param>
        /// <param name="deltaFileLocation">Where to output the delta file</param>
        /// <param name="extension">What extension to know it was made using this when applying the delta</param>
        /// <returns>If we was able to create the delta file</returns>
        private static bool CreateMSDiffFile(string baseFileLocation, string newFileLocation, string deltaFileLocation, out string extension)
        {
            //Here just to quickly test stuff
            return CreateBSDiffFile(baseFileLocation, newFileLocation, deltaFileLocation, out extension);
            
            extension = ".diff";
            var msDelta = new MsDeltaCompression();
            try
            {
                msDelta.CreateDelta(baseFileLocation, newFileLocation, deltaFileLocation);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Adds a file that is not in the last version into the <see cref="ZipArchive"/>
        /// </summary>
        /// <param name="zipArchive"><see cref="ZipArchive"/> to add the file too</param>
        /// <param name="fileStream">Files stream to add</param>
        /// <param name="filepath">File to add</param>
        /// <returns>If we was able to add the file</returns>
        private static async Task<bool> AddNewFile(ZipArchive zipArchive, Stream fileStream, string filepath) =>
            await AddFile(zipArchive, fileStream, filepath + ".new", false); //We give 0 for filesize or it will make a shasum file
        //Give hash
        
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
        private static async Task<bool> AddFile(ZipArchive zipArchive, Stream fileStream, string filepath, bool keepFileStreamOpen = true, long? filesize = null, string? sha1hash = null)
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

            sha1hash ??= SHA1Util.CreateSHA1Hash(fileStream);
            //Create .shasum file if we have some content from the fileStream
            var zipShasumStream = zipArchive.CreateEntry(Path.ChangeExtension(filepath, ".shasum")).Open();
            var textWriter = new StreamWriter(zipShasumStream);

            await textWriter.WriteAsync($"{sha1hash} {filesize}");
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