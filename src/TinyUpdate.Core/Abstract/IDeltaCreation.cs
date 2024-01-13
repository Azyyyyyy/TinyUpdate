namespace TinyUpdate.Core.Abstract;

/// <summary>
/// Provides base functionality for creating delta updates 
/// </summary>
public interface IDeltaCreation : IExtension
{
    /// <summary>
    /// Creates an delta update
    /// </summary>
    /// <param name="sourceStream">Source stream (Previous data)</param>
    /// <param name="targetStream">Target stream (New data)</param>
    /// <param name="deltaStream">Delta stream (Patch data - OUTPUT)</param>
    /// <param name="progress">Progress on creating the delta</param>
    /// <returns>If creating the delta was successful</returns>
    Task<bool> CreateDeltaFile(Stream sourceStream, Stream targetStream, Stream deltaStream, IProgress<double>? progress = null);
}