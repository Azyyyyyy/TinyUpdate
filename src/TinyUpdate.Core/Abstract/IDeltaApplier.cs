namespace TinyUpdate.Core.Abstract;

/// <summary>
/// Provides base functions for applying an delta update
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
    /// <param name="sourceFileStream">Source stream</param>
    /// <param name="deltaFileStream">Delta stream</param>
    /// <param name="targetFileStream">Target stream (OUTPUT)</param>
    /// <param name="progress">Progress on applying delta update</param>
    /// <returns>If applying the update was successful</returns>
    Task<bool> ApplyDeltaFile(Stream sourceFileStream, Stream deltaFileStream, Stream targetFileStream, IProgress<double>? progress = null);
}