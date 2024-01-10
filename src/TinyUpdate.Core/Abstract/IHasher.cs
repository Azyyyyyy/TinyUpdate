namespace TinyUpdate.Core.Abstract;

public interface IHasher
{
    /// <summary>
    /// Checks the output of a <see cref="Stream"/> to a hash that is expected
    /// </summary>
    /// <param name="stream"><see cref="Stream"/> to check against</param>
    /// <param name="expectedHash">Hash that we are expecting</param>
    /// <returns>If the <see cref="Stream"/> outputs the same hash as we are expecting</returns>
    bool CompareHash(Stream stream, string expectedHash);

    /// <summary>
    /// Checks a <see cref="byte"/>[] to a hash that is expected
    /// </summary>
    /// <param name="byteArray"><see cref="byte"/>[] to check against</param>
    /// <param name="expectedHash">Hash that we are expecting</param>
    /// <returns>If the <see cref="byte"/>[] outputs the same hash as we are expecting</returns>
    bool CompareHash(byte[] byteArray, string expectedHash);

    /// <summary>
    /// Creates a hash from a <see cref="Stream"/>
    /// </summary>
    /// <param name="stream"><see cref="Stream"/> to create hash for</param>
    string HashData(Stream stream);

    /// <summary>
    /// Creates a hash from a <see cref="byte"/>[]
    /// </summary>
    /// <param name="bytes"><see cref="byte"/>[] to use for creating hash</param>
    string HashData(byte[] bytes);
    
    /// <summary>
    /// Gets if this string is a valid hash  
    /// </summary>
    /// <param name="hash">string to check</param>
    bool IsValidHash(string hash);
}