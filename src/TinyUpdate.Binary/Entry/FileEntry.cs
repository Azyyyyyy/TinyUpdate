using System.IO;

namespace TinyUpdate.Binary.Entry
{
    /// <summary>
    /// File entry with all the information to do a update
    /// </summary>
    internal class FileEntry
    {
        public FileEntry(string filename, string? folderPath)
        {
            Filename = filename;
            FolderPath = folderPath;
            FileLocation = string.IsNullOrWhiteSpace(folderPath) ? 
                Filename : 
                Path.Combine(folderPath, Filename);
        }
        
        /// <summary>
        /// The filename of the file
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// The folder path for <see cref="Filename"/>
        /// </summary>
        public string? FolderPath { get; }

        /// <summary>
        /// The relative path of <see cref="Filename"/>
        /// </summary>
        public string FileLocation { get; }

        /// <summary>
        /// The SHA256 hash that <see cref="Filename"/> is expected to be once applied to disk
        /// </summary>
        public string? SHA256 { get; set; }

        /// <summary>
        /// The size that <see cref="Filename"/> is expected to be once applied to disk
        /// </summary>
        public long Filesize { get; set; }

        /// <summary>
        /// <see cref="Filename"/> <see cref="System.IO.Stream"/>
        /// </summary>
        public Stream? Stream { get; set; }

        /// <summary>
        /// What kind of patch this is
        /// </summary>
        public PatchType PatchType { get; set; }
    }

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