namespace TinyUpdate.Core.Abstract;

public interface IDeltaCreation : IExtension
{
    /// <summary>
    /// Creates the delta file
    /// </summary>
    /// <param name="sourceFileStream">Source stream</param>
    /// <param name="targetFileStream">Target stream</param>
    /// <param name="deltaFileStream">Delta stream (OUTPUT)</param>
    /// <param name="progress">Progress on creating the delta</param>
    /// <returns>If creating the delta was successful</returns>
    Task<bool> CreateDeltaFile(Stream sourceFileStream, Stream targetFileStream, Stream deltaFileStream, IProgress<double>? progress = null);
}