using System;
using System.IO;
using System.Runtime.InteropServices;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Binary.Delta
{
    /// <summary>
    /// Creates a delta file for two versions of a file 
    /// </summary>
    public static class DeltaCreation
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(DeltaCreation));
        
        /// <summary>
        /// Creates a delta file by going through the different ways of creating delta files
        /// </summary>
        /// <param name="baseFileLocation">Where the older version of the file exists</param>
        /// <param name="newFileLocation">Where the newer version of the file exists</param>
        /// <param name="deltaFileLocation">Where the delta file should be stored (If we are unable to store it in a stream)</param>
        /// <param name="extension">Extension of the delta file</param>
        /// <param name="deltaFileStream">The contents of the delta file</param>
        /// <param name="progress">Progress of making the delta file (If we can report the progress back)</param>
        public static bool CreateDeltaFile(
            string baseFileLocation, 
            string newFileLocation, 
            string deltaFileLocation, 
            out string extension, 
            out Stream? deltaFileStream, 
            Action<decimal>? progress = null)
        {
            deltaFileStream = null;
            if (CreateMSDiffFile(baseFileLocation, newFileLocation, deltaFileLocation, out extension) ||
                CreateBSDiffFile(baseFileLocation, newFileLocation, out extension, out deltaFileStream, progress))
            {
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Creates a delta file using <see cref="BinaryPatchUtility.Create"/>
        /// </summary>
        /// <param name="baseFileLocation">Old file location</param>
        /// <param name="newFileLocation">New file location</param>
        /// <param name="extension">What extension to know it was made using this when applying the delta</param>
        /// <param name="deltaFileStream">Stream with the </param>
        /// <param name="progress">Reports back progress</param>
        /// <returns>If we was able to create the delta file</returns>
        internal static bool CreateBSDiffFile(
            string baseFileLocation, 
            string newFileLocation,
            out string extension,
            out Stream? deltaFileStream,
            Action<decimal>? progress = null)
        {
            extension = ".bsdiff";
            var tmpDeltaFileStream = new MemoryStream();

            var success = BinaryPatchUtility.Create(
                File.ReadAllBytes(baseFileLocation),
                File.ReadAllBytes(newFileLocation), 
                tmpDeltaFileStream, progress);
            deltaFileStream = tmpDeltaFileStream;

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
        internal static bool CreateMSDiffFile(
            string baseFileLocation, 
            string newFileLocation, 
            string deltaFileLocation, 
            out string extension)
        {
            extension = ".diff";
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Logger.Error("We aren't on Windows so can't apply MSDiff update");
                return false;
            }
            
            return MsDelta.MsDelta.CreateDelta(baseFileLocation, newFileLocation, deltaFileLocation);
        }
    }
}