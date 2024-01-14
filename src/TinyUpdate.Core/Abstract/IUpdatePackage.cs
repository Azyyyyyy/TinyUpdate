using TinyUpdate.Core.Model;

namespace TinyUpdate.Core.Abstract;

/// <summary>
/// Provides base functionality for handling update packages
/// </summary>
public interface IUpdatePackage : IExtension
{
    /// <summary>
    /// Loads the update package data in
    /// </summary>
    /// <param name="updatePackageStream">Data to load in</param>
    Task Load(Stream updatePackageStream);
    
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
}