using System;
using System.IO;
using System.Runtime.InteropServices;
using TinyUpdate.Binary.Delta.MsDelta;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Binary.Delta
{
    /// <summary>
    /// Creates a delta file for two versions of a file 
    /// </summary>
    public static class DeltaCreation
    {
        //TODO: Change how we grab theses
        internal static readonly IDeltaUpdate[] DeltaUpdaters =
        {
            new MsDiff(),
            new BSDiff()
        };

        /// <summary>
        /// Creates a delta file by going through the different ways of creating delta files
        /// </summary>
        /// <param name="baseFileLocation">Where the older version of the file exists</param>
        /// <param name="newFileLocation">Where the newer version of the file exists</param>
        /// <param name="deltaFileLocation">Where the delta file should be stored (If we are unable to store it in a stream)</param>
        /// <param name="intendedOs">What OS this delta file will be intended for</param>
        /// <param name="extension">Extension of the delta file</param>
        /// <param name="deltaFileStream">The contents of the delta file</param>
        /// <param name="progress">Progress of making the delta file (If we can report the progress back)</param>
        public static bool CreateDeltaFile(
            string baseFileLocation,
            string newFileLocation,
            string deltaFileLocation,
            OSPlatform? intendedOs,
            out string extension,
            out Stream? deltaFileStream,
            Action<decimal>? progress = null)
        {
            extension = "";
            deltaFileStream = null;
            foreach (var deltaUpdater in DeltaUpdaters)
            {
                /*Skip if we know that this creator is not going
                 to work on the intended OS*/
                if (intendedOs != null
                    && deltaUpdater.IntendedOs != null
                    && intendedOs != deltaUpdater.IntendedOs)
                {
                    continue;
                }

                if (deltaUpdater.CreateDeltaFile(baseFileLocation, newFileLocation, deltaFileLocation,
                    out deltaFileStream, progress))
                {
                    extension = deltaUpdater.Extension;
                    return true;
                }
            }

            return false;
        }
    }
}