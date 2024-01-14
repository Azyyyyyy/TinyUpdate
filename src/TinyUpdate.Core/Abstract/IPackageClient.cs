namespace TinyUpdate.Core.Abstract;

/// <summary>
/// Handles getting updates from a source
/// </summary>
public interface IPackageClient
{
    /// <summary>
    /// Gets all the release entries which can be applied to the application
    /// </summary>
    Task<IEnumerable<ReleaseEntry>> GetUpdates();
    
    /// <summary>
    /// Downloads a single update
    /// </summary>
    /// <param name="releaseEntry">Entry which contains data about release</param>
    /// <param name="progress">Progress of downloading update</param>
    /// <returns>If downloading the update was successful</returns>
    Task<bool> DownloadUpdate(ReleaseEntry releaseEntry, IProgress<double>? progress);

    /// <summary>
    /// Applies a single update
    /// </summary>
    /// <param name="releaseEntry">Entry which contains data about release</param>
    /// <param name="progress">Progress of applying update</param>
    /// <returns>If applying the update was successful</returns>
    Task<bool> ApplyUpdate(ReleaseEntry releaseEntry, IProgress<double>? progress);
}