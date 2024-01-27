using TinyUpdate.Core.Abstract.UpdatePackage;

namespace TinyUpdate.Core.Abstract;

//TODO: Find anywhere which make use of IProgress and see what actually needs hooking up to it
/// <summary>
/// Provides base functions for applying update(s)
/// </summary>
public interface IUpdateApplier
{
    /// <summary>
    /// Detects if the update(s) can be applied on the running OS
    /// </summary>
    bool SupportedOS();

    /// <summary>
    /// Apply one update
    /// </summary>
    /// <returns>If the update was successful</returns>
    Task<bool> ApplyUpdate(IUpdatePackage updatePackage, string applicationLocation, IProgress<double>? progress = null);
    
    /// <summary>
    /// Apply many updates
    /// </summary>
    /// <returns>If the updates was successful</returns>
    Task<bool> ApplyUpdates(ICollection<IUpdatePackage> updatePackages, string applicationLocation,
        IProgress<double>? progress = null);

    /// <summary>
    /// Cleanup old application builds
    /// </summary>
    /// <returns>If cleanup was successful</returns>
    Task<bool> Cleanup();
}