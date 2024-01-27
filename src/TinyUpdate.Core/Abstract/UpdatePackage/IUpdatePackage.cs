using System.Diagnostics.CodeAnalysis;
using TinyUpdate.Core.Model;

namespace TinyUpdate.Core.Abstract.UpdatePackage;

public record LoadResult
{
    public static LoadResult Failed(string message) => new() { Successful = false, Message = message };

    public static readonly LoadResult Success = new() { Successful = true };

    [MemberNotNullWhen(false, nameof(Message))]
    public bool Successful { get; protected init; }

    public string? Message { get; protected init; }
}

/// <summary>
/// Provides base functionality for handling update packages
/// </summary>
public interface IUpdatePackage : IExtension
{
    /// <summary>
    /// Loads the update package data in
    /// </summary>
    /// <param name="updatePackageStream">Data to load in</param>
    /// <param name="releaseEntry">Entry that is related to this <see cref="IUpdatePackage"/></param>
    Task<LoadResult> Load(Stream updatePackageStream, ReleaseEntry releaseEntry);

    /// <summary>
    /// Entry that is related to this <see cref="IUpdatePackage"/>
    /// </summary>
    ReleaseEntry ReleaseEntry { get; }

    /// <summary>
    /// Files that have been processed into a delta file
    /// </summary>
    IReadOnlyCollection<FileEntry> DeltaFiles { get; }

    /// <summary>
    /// Files that should already be on the device
    /// </summary>
    IReadOnlyCollection<FileEntry> UnchangedFiles { get; }

    /// <summary>
    /// Files that aren't in the last update 
    /// </summary>
    IReadOnlyCollection<FileEntry> NewFiles { get; }
    
    /// <summary>
    /// Files that are unchanged but moved 
    /// </summary>
    IReadOnlyCollection<FileEntry> MovedFiles { get; }

    /// <summary>
    /// All the directories that this will require to exist
    /// </summary>
    IReadOnlyCollection<string> Directories { get; }

    /// <summary>
    /// How many files are contained in this <see cref="IUpdatePackage"/>
    /// </summary>
    long FileCount { get; }
}