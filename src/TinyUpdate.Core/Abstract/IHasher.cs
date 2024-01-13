namespace TinyUpdate.Core.Abstract;

/// <summary>
/// Provides base functionality to create & check hashes from a <see cref="Stream"/> or <see cref="byte"/>[]
/// </summary>
public interface IHasher
{
    /// <summary>
    /// Compares the <see cref="Stream"/> with an expected hash
    /// </summary>
    /// <param name="stream"><see cref="Stream"/> to check</param>
    /// <param name="expectedHash">Hash that we are expecting</param>
    /// <returns>If the <see cref="Stream"/> outputs the same hash as the expected hash</returns>
    bool CompareHash(Stream stream, string expectedHash);

    /// <summary>
    /// Compares the <see cref="byte"/>[] with an expected hash
    /// </summary>
    /// <param name="byteArray"><see cref="byte"/>[] to check</param>
    /// <param name="expectedHash">Hash that we are expecting</param>
    /// <returns>If the <see cref="byte"/>[] outputs the same hash as the expected hash</returns>
    bool CompareHash(byte[] byteArray, string expectedHash);

    /// <summary>
    /// Creates a hash from a <see cref="Stream"/>
    /// </summary>
    /// <param name="stream"><see cref="Stream"/> to create the hash from</param>
    string HashData(Stream stream);

    /// <summary>
    /// Creates a hash from a <see cref="byte"/>[]
    /// </summary>
    /// <param name="byteArray"><see cref="byte"/>[] to create the hash from</param>
    string HashData(byte[] byteArray);
    
    /// <summary>
    /// Ensures that the <see cref="string"/> is a valid hash
    /// </summary>
    bool IsValidHash(string hash);
}