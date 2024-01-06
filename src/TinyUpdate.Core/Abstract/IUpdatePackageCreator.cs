using SemVersion;

namespace TinyUpdate.Core.Abstract;

public interface IUpdatePackageCreator : IExtension
{
    public string FullPackageFilenameTemplate { get; }
    public string DeltaPackageFilenameTemplate { get; }

    Task<bool> CreateFullPackage(string applicationLocation, SemanticVersion applicationVersion, string updatePackageLocation, string applicationName, IProgress<double>? progress = null);
    
    Task<bool> CreateDeltaPackage(string oldApplicationLocation, SemanticVersion oldApplicationVersion, string newApplicationLocation, SemanticVersion newApplicationVersion, string updatePackageLocation, string applicationName, IProgress<double>? progress = null);
}