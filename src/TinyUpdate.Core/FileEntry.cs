using System.Text.Json.Serialization;

namespace TinyUpdate.Core;

/// <summary>
/// File entry with all the information to do a update
/// </summary>
public class FileEntry
{
    [JsonConstructor]
    protected FileEntry(string filename, string path, string? previousLocation, string hash, long filesize, string extension)
        : this(filename, path)
    {
        PreviousLocation = previousLocation;
        Hash = hash;
        Filesize = filesize;
        Extension = extension;
    }
    
    public FileEntry(string filename, string path)
    {
        Filename = filename;
        Path = path;
        Location = string.IsNullOrWhiteSpace(path) ? Filename : System.IO.Path.Combine(path, Filename);
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
    [JsonIgnore]
    public string Location { get; }

    /// <summary>
    /// The relative path of the file within the old version
    /// </summary>
    public string? PreviousLocation { get; init; }

    /// <summary>
    /// The hash that <see cref="Filename"/> is expected to be once applied to disk
    /// </summary>
    public required string Hash { get; init; }

    /// <summary>
    /// The size that <see cref="Filename"/> is expected to be once applied to disk
    /// </summary>
    public required long Filesize { get; init; }

    /// <summary>
    /// <see cref="Filename"/> <see cref="System.IO.Stream"/>
    /// </summary>
    [JsonIgnore]
    public Stream? Stream { get; init; }

    /// <summary>
    /// The extension that exposes how to process this file
    /// </summary>
    /// <remarks>
    /// .new -> "New File"
    /// </remarks>
    public required string Extension { get; init; }
}