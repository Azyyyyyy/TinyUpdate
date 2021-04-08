using TinyUpdate.Binary.Entry;

namespace TinyUpdate.Binary.Extensions
{
    internal static class FileEntryExt
    {
        /// <summary>
        /// Checks that this file is a delta file 
        /// </summary>
        /// <param name="fileEntry">Details about the file</param>
        public static bool IsDeltaFile(this FileEntry fileEntry)
        {
            return fileEntry.Filesize != 0 && fileEntry.PatchType != PatchType.New;
        }

        /// <summary>
        /// Checks that this file is a new file 
        /// </summary>
        /// <param name="fileEntry">Details about the file</param>
        public static bool IsNewFile(this FileEntry fileEntry)
        {
            return fileEntry.Filesize != 0 && fileEntry.PatchType == PatchType.New;
        }
    }
}