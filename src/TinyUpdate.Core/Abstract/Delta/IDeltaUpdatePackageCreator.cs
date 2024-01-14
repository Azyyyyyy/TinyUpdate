using SemVersion;

namespace TinyUpdate.Core.Abstract.Delta;

/// <summary>
/// Provides base functionality for creating delta update packages
/// </summary>
public interface IDeltaUpdatePackageCreator : IExtension
{
    /// <summary>
    /// Template for a delta package filename (Null if not supported)
    /// </summary>
    string DeltaPackageFilenameTemplate { get; }
    
    /// <summary>
    /// Creates a delta update package
    /// </summary>
    /// <param name="previousApplicationLocation">Where the previous version of the application is located</param>
    /// <param name="previousApplicationVersion">What the previous version of the application is</param>
    /// <param name="newApplicationLocation">Where the new version of the application is located</param>
    /// <param name="newApplicationVersion">What the new version of the application is</param>
    /// <param name="updatePackageLocation">Where we should store the created update package</param>
    /// <param name="applicationName">The applications name</param>
    /// <param name="progress">Process about creating the update package</param>
    /// <returns>If we successfully created the update package</returns>
    Task<bool> CreateDeltaPackage(string previousApplicationLocation, SemanticVersion previousApplicationVersion, string newApplicationLocation, SemanticVersion newApplicationVersion, string updatePackageLocation, string applicationName, IProgress<double>? progress = null);
}