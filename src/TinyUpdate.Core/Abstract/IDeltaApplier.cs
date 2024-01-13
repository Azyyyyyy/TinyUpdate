namespace TinyUpdate.Core.Abstract;

/// <summary>
/// Provides base functionality for applying delta updates
/// </summary>
public interface IDeltaApplier : IExtension
{
    /// <summary>
    /// Checks if the stream can be processed by this applier
    /// </summary>
    bool SupportedStream(Stream deltaStream);
    
    /// <summary>
    /// Gets what the target size will be once the delta has been applied (-1 if not supported)
    /// </summary>
    long TargetStreamSize(Stream deltaStream);
    
    /// <summary>
    /// Applies the delta update into the target stream
    /// </summary>
    /// <param name="sourceStream">Source stream (Previous data)</param>
    /// <param name="deltaStream">Delta stream (Patch data)</param>
    /// <param name="targetStream">Target stream (New data - OUTPUT)</param>
    /// <param name="progress">Progress on applying delta update</param>
    /// <returns>If applying the update was successful</returns>
    Task<bool> ApplyDeltaFile(Stream sourceStream, Stream deltaStream, Stream targetStream, IProgress<double>? progress = null);
}