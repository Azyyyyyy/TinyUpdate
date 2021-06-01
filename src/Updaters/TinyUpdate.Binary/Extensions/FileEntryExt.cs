using TinyUpdate.Binary.Entry;

namespace TinyUpdate.Binary.Extensions
{
    /// <summary>
    /// Extensions to make grabbing data from <see cref="FileEntry"/>'s easier
    /// </summary>
    internal static class FileEntryExt
    {
        /// <summary>
        /// Checks that this file is a delta file 
        /// </summary>
        /// <param name="fileEntry">Details about the file</param>
        public static bool IsDeltaFile(this FileEntry fileEntry)
        {
            return fileEntry.Filesize != 0 
                   && fileEntry.DeltaExtension != ".new"
                   && fileEntry.DeltaExtension != ".load";
        }

        /// <summary>
        /// Checks that this file is a new file 
        /// </summary>
        /// <param name="fileEntry">Details about the file</param>
        public static bool IsNewFile(this FileEntry fileEntry)
        {
            return fileEntry.Filesize != 0 && fileEntry.DeltaExtension == ".new";
        }
    }
}