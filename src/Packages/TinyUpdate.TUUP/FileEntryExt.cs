using TinyUpdate.Core;
using TinyUpdate.Core.Model;

namespace TinyUpdate.TUUP;

/// <summary>
/// Extensions to make grabbing data from <see cref="FileEntry"/>'s easier
/// </summary>
internal static class FileEntryExt
{
    /// <summary>
    /// Checks if the file is a delta file 
    /// </summary>
    /// <param name="fileEntry">Details about the file</param>
    public static bool IsDeltaFile(this FileEntry fileEntry) => fileEntry.Filesize != 0 && fileEntry.Extension != Consts.NewFileExtension && fileEntry.Extension != Consts.MovedFileExtension;

    /// <summary>
    /// Checks if the file is new 
    /// </summary>
    /// <param name="fileEntry">Details about the file</param>
    public static bool IsNewFile(this FileEntry fileEntry) => fileEntry.Filesize != 0 && fileEntry.Extension == Consts.NewFileExtension;
    
    /// <summary>
    /// Checks if the file has moved
    /// </summary>
    /// <param name="fileEntry">Details about the file</param>
    public static bool HasFileMoved(this FileEntry fileEntry) => fileEntry.Filesize != 0 && fileEntry.Extension == Consts.MovedFileExtension;
}