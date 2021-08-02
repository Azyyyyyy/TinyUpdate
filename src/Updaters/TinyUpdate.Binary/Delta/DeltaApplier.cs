using System;
using System.Threading.Tasks;
using TinyUpdate.Binary.Entry;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Temporary;

namespace TinyUpdate.Binary.Delta
{
    /// <summary>
    /// Processes applying delta update files 
    /// </summary>
    public static class DeltaApplier
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(DeltaApplier));

        /// <summary>
        /// Processes a file that has a delta update
        /// </summary>
        /// <param name="tempFolder">Where the temp folder is located</param>
        /// <param name="originalFile">Where the original file is</param>
        /// <param name="newFile">Where the updated file should be placed on disk</param>
        /// <param name="file">Details about the update</param>
        /// <param name="progress">Progress for applying the update</param>
        /// <returns>If the file was successfully updated</returns>
        public static async Task<bool> ProcessDeltaFile(TemporaryFolder tempFolder, string originalFile, string newFile, FileEntry file, Action<double>? progress = null)
        {
            if (file.Stream == null)
            {
                Logger.Warning("{0} was updated but we don't have the delta stream for it!", file.Filename);
                return false;
            }
            Logger.Debug("File was updated, applying delta update");

            var deltaUpdater = DeltaUpdaters.GetUpdater(file.DeltaExtension);
            if (deltaUpdater != null)
            {
                return await deltaUpdater.ApplyDeltaFile(tempFolder, originalFile, newFile, file.Filename, file.Stream, progress);
            }

            Logger.Error("Wasn't able to find what was responsible for creating this diff file (File: {0}, Extension: {1})", file.Filename, file.DeltaExtension);
            return false;
        }
    }
}