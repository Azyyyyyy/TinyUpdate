using TinyUpdate.Core.Abstract;

namespace TinyUpdate.Core;

/// <summary>
/// File entry with all the information to do a update
/// </summary>
public class FileEntry
{
    public FileEntry(string filename, string? folderPath)
    {
        Filename = filename;
        Path = folderPath;
        Location = string.IsNullOrWhiteSpace(folderPath) ? Filename : System.IO.Path.Combine(folderPath, Filename);
    }

    /// <summary>
    /// The filename of the file
    /// </summary>
    public string Filename { get; }

    /// <summary>
    /// The folder path for <see cref="Filename"/>
    /// </summary>
    public string? Path { get; }

    /// <summary>
    /// The relative path of <see cref="Filename"/>
    /// </summary>
    public string Location { get; }

    /// <summary>
    /// The SHA256 hash that <see cref="Filename"/> is expected to be once applied to disk
    /// </summary>
    public required string SHA256 { get; init; }

    /// <summary>
    /// The size that <see cref="Filename"/> is expected to be once applied to disk
    /// </summary>
    public required long Filesize { get; init; }

    /// <summary>
    /// <see cref="Filename"/> <see cref="System.IO.Stream"/>
    /// </summary>
    public required Stream? Stream { get; init; }

    /// <summary>
    /// The extension that exposes how to process this file
    /// </summary>
    /// <remarks>
    /// .new -> "New File"
    /// </remarks>
    public required string Extension { get; init; }
}