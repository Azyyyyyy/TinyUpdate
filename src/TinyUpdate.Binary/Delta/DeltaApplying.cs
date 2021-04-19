using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TinyUpdate.Binary.Entry;
using TinyUpdate.Core;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Binary.Delta
{
    /// <summary>
    /// Processes applying delta update files 
    /// </summary>
    public static class DeltaApplying
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(DeltaApplying));

        /// <summary>
        /// Processes a file that has a delta update
        /// </summary>
        /// <param name="originalFile">Where the original file is</param>
        /// <param name="newFile">Where the file file needs to be</param>
        /// <param name="file">Details about how we the update was made</param>
        /// <param name="progress">Progress of applying update</param>
        /// <returns>If we was able to process the file</returns>
        public static async Task<bool> ProcessDeltaFile(string originalFile, string newFile, FileEntry file, Action<decimal>? progress = null)
        {
            Logger.Debug("File was updated, applying delta update");
            return await (file.PatchType switch
            {
                PatchType.MSDiff => ApplyMSDiff(file, newFile, originalFile),
                PatchType.BSDiff => ApplyBSDiff(file, newFile, originalFile, progress),
                _ => Task.FromResult(false)
            });
        }
        
        /// <summary>
        /// Applies a patch that was created using <see cref="MsDelta"/>
        /// </summary>
        /// <param name="fileEntry">Patch to apply</param>
        /// <param name="outputLocation">Where the output file should be</param>
        /// <param name="baseFile">Where the original file is</param>
        /// <returns>If the patch was applied correctly</returns>
        internal static async Task<bool> ApplyMSDiff(FileEntry fileEntry, string outputLocation, string baseFile)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Logger.Error("We aren't on Windows so can't apply MSDiff update");
                return false;
            }
            
            //Create Temp folder if it doesn't exist
            Directory.CreateDirectory(Global.TempFolder);
            
            var tmpDeltaFile = Path.Combine(Global.TempFolder, fileEntry.Filename);

            //Delete the tmp file if it already exists, likely from the last update
            if (File.Exists(tmpDeltaFile))
            {
                File.Delete(tmpDeltaFile);
            }

            if (fileEntry.Stream == null)
            {
                Logger.Error("fileEntry's doesn't have a stream, can't make MSDiff update");
                return false;
            }
            
            //Put the delta file onto disk
            var tmpFileStream = File.OpenWrite(tmpDeltaFile);
            await fileEntry.Stream.CopyToAsync(tmpFileStream);
            tmpFileStream.Dispose();
            
            //If baseFile + outputLocation are the same, copy it to a tmp file
            //and then give it that (deleting it after)
            string? tmpBaseFile = null;
            if (baseFile == outputLocation)
            {
                tmpBaseFile = Path.Combine(Global.TempFolder, Path.GetRandomFileName());
                File.Copy(baseFile, tmpBaseFile);
                baseFile = tmpBaseFile;
            }
            
            //Make the updated file!
            File.Create(outputLocation).Dispose();
            var wasApplySuccessful = MsDelta.MsDelta.ApplyDelta(tmpDeltaFile, baseFile, outputLocation);

            //Delete tmp files
            File.Delete(tmpDeltaFile);
            if (!string.IsNullOrWhiteSpace(tmpBaseFile))
            {
                File.Delete(tmpBaseFile);
            }
            
            return wasApplySuccessful;
        }

        /// <summary>
        /// Applies a patch that was created using <see cref="BinaryPatchUtility"/>
        /// </summary>
        /// <inheritdoc cref="ApplyMSDiff"/>
        /// <param name="progress">Progress of applying update</param>
        /// <param name="fileEntry"></param>
        /// <param name="outputLocation"></param>
        /// <param name="baseFile"></param>
        internal static async Task<bool> ApplyBSDiff(FileEntry fileEntry, string outputLocation, string baseFile, Action<decimal>? progress)
        {
            Stream? inputStream = null;
            /*If this is the same file then we want to copy it to mem and not
             read from disk*/
            if (outputLocation == baseFile)
            {
                inputStream = new MemoryStream();
                var fileStream = StreamUtil.SafeOpenRead(baseFile);
                if (fileStream == null)
                {
                    Logger.Error("Wasn't able to grab {0} for applying a BSDiff update", baseFile);
                    return false;
                }
                await fileStream.CopyToAsync(inputStream);
                fileStream.Dispose();
                inputStream.Seek(0, SeekOrigin.Begin);
            }
            
            //Create streams for old file and where the new file will be
            var outputStream = File.OpenWrite(outputLocation);
            inputStream ??= StreamUtil.SafeOpenRead(baseFile);
            
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
            }, outputStream, progress);

            outputStream.Dispose();
            inputStream.Dispose();
            return successfulUpdate;
        }
    }
}