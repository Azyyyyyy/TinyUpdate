namespace TinyUpdate.TUUP;

public static class Consts
{
    /// <summary>
    /// The Tuup file extension!
    /// </summary>
    public const string TuupExtension = ".tuup";
    
    /// <summary>
    /// Extension to indicate the file is a brand new file
    /// </summary>
    public const string NewFileExtension = ".new";

    /// <summary>
    /// Extension to indicate the file has moved
    /// </summary>
    public const string MovedFileExtension = ".moved";
    
    /// <summary>
    /// Extension to indicate this file contains hash details about another file
    /// </summary>
    public const string ShasumFileExtension = ".shasum";
    
    /// <summary>
    /// Extension to indicate the file has unchanged
    /// </summary>
    public const string UnchangedFileExtension = ".diff";
}