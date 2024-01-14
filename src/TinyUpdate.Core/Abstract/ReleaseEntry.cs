namespace TinyUpdate.Core.Abstract;

/// <summary>
/// Provides data about a release
/// </summary>
public abstract class ReleaseEntry
{
    /// <summary>
    /// If this entry contains an update to be applied
    /// </summary>
    public abstract bool HasUpdate { get; }
}