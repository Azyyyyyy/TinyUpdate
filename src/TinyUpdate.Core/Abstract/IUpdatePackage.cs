using SemVersion;
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
    /// <param name="previousVersion">What version this update package was created against</param>
    /// <param name="newVersion">What version this update package will bump the application too</param>
    Task Load(Stream updatePackageStream, SemanticVersion previousVersion, SemanticVersion newVersion);

    /// <summary>
    /// What version this update package will bump the application too
    /// </summary>
    SemanticVersion NewVersion { get; }

    /// <summary>
    /// What version this update package was created against
    /// </summary>
    SemanticVersion PreviousVersion { get; }

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