using SemVersion;

namespace TinyUpdate.Core.Abstract.UpdatePackage;

/// <summary>
/// Provides base functionality for creating update packages
/// </summary>
public interface IUpdatePackageCreator : IExtension
{
    /// <summary>
    /// Template for a full package filename
    /// </summary>
    string FullPackageFilenameTemplate { get; }

    /// <summary>
    /// Creates a full update package
    /// </summary>
    /// <param name="applicationLocation">Where the application files are located</param>
    /// <param name="applicationVersion">What the application version is</param>
    /// <param name="updatePackageLocation">Where we should store the created update package</param>
    /// <param name="applicationName">The applications name</param>
    /// <param name="progress">Process about creating the update package</param>
    /// <returns>If we successfully created the update package</returns>
    Task<bool> CreateFullPackage(string applicationLocation, SemanticVersion applicationVersion, string updatePackageLocation, string applicationName, IProgress<double>? progress = null);
}