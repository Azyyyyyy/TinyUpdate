using TinyUpdate.Core;

namespace TinyUpdate.TUUP;

/// <summary>
/// Extensions to make grabbing data from <see cref="FileEntry"/>'s easier
/// </summary>
internal static class FileEntryExt
{
    /// <summary>
    /// Checks that this file is a delta file 
    /// </summary>
    /// <param name="fileEntry">Details about the file</param>
    public static bool IsDeltaFile(this FileEntry fileEntry) => fileEntry.Filesize != 0 && fileEntry.Extension != ".new" && fileEntry.Extension != ".moved";

    /// <summary>
    /// Checks that this file is a new file 
    /// </summary>
    /// <param name="fileEntry">Details about the file</param>
    public static bool IsNewFile(this FileEntry fileEntry) => fileEntry.Filesize != 0 && fileEntry.Extension == ".new";
    
    /// <summary>
    /// Checks that this file has moved
    /// </summary>
    /// <param name="fileEntry">Details about the file</param>
    public static bool HasFileMoved(this FileEntry fileEntry) => fileEntry.Filesize != 0 && fileEntry.Extension == ".moved";
}