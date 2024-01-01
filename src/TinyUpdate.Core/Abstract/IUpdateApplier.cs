namespace TinyUpdate.Core.Abstract;

//TODO: Imp
/// <summary>
/// Provides base functions for applying update(s)
/// </summary>
public interface IUpdateApplier
{
    /// <summary>
    /// Detects the current OS running and provides if update(s) can be applied
    /// </summary>
    bool SupportedOS();

    /// <summary>
    /// Apply one update
    /// </summary>
    /// <returns>If the update was successful</returns>
    Task<bool> ApplyUpdate();
    
    /// <summary>
    /// Apply many updates
    /// </summary>
    /// <returns>If the updates was successful</returns>
    Task<bool> ApplyUpdates();

    /// <summary>
    /// Cleanup old application builds
    /// </summary>
    /// <returns>If cleanup was successful</returns>
    Task<bool> Cleanup();
}