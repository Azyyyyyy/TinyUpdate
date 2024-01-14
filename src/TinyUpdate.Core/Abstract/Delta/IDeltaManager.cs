using TinyUpdate.Core.Model;

namespace TinyUpdate.Core.Abstract.Delta;

/// <summary>
/// Manages delta processing for external packages
/// </summary>
public interface IDeltaManager
{
    /// <summary>
    /// The <see cref="IDeltaApplier"/>s which will be used for applying delta updates
    /// </summary>
    public IReadOnlyCollection<IDeltaApplier> Appliers { get; }

    /// <summary>
    /// The <see cref="IDeltaCreation"/>s which will be used for creating delta updates
    /// </summary>
    public IReadOnlyCollection<IDeltaCreation> Creators { get; }

    /// <summary>
    /// Applies the delta update
    /// </summary>
    /// <param name="fileEntry"></param>
    /// <param name="sourceStream">Source Stream (Previous data)</param>
    /// <param name="targetStream"></param>
    /// <returns></returns>
    Task<bool> ApplyDeltaUpdate(FileEntry fileEntry, Stream sourceStream, Stream targetStream);
    
    /// <summary>
    /// Creates a delta update, returning the best delta created
    /// </summary>
    /// <param name="sourceStream">Source Stream (Previous data)</param>
    /// <param name="targetStream">Target Stream (New data)</param>
    /// <returns>The results of delta creation</returns>
    public Task<DeltaCreationResult> CreateDeltaUpdate(Stream sourceStream, Stream targetStream);
}