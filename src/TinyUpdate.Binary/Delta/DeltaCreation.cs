using System;
using System.IO;
using System.Runtime.InteropServices;
using DeltaCompressionDotNet.MsDelta;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Binary.Delta
{
    public class DeltaCreation
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(DeltaCreation));
        
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
                CreateBSDiffFile(baseFileLocation, newFileLocation, deltaFileLocation, out extension, out deltaFileStream, progress))
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

            var success = BinaryPatchUtility.Create(File.ReadAllBytes(baseFileLocation),
                File.ReadAllBytes(newFileLocation), deltaFileStream, progress);

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
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Logger.Error("We aren't on Windows so can't apply MSDiff update");
                return false;
            }

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
    }
}