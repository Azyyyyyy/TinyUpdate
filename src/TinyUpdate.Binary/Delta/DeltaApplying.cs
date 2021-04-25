using System;
using System.Linq;
using System.Threading.Tasks;
using TinyUpdate.Binary.Entry;
using TinyUpdate.Core.Logging;

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
        public static async Task<bool> ProcessDeltaFile(string originalFile, string newFile, FileEntry file,
            Action<decimal>? progress = null)
        {
            Logger.Debug("File was updated, applying delta update");

            var deltaUpdater = DeltaCreation.DeltaUpdaters.First(x => x.Extension == file.DeltaExtension);
            return await deltaUpdater.ApplyDeltaFile(originalFile, newFile, file.Filename, file.Stream, progress);
        }
    }
}