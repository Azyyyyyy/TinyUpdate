using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable MemberCanBePrivate.Global

namespace TinyUpdate.Core;

//TODO: Move over to make this more generic (allowing different imps of SHA to be used)
/// <summary>
/// Easy access to processing Streams to get a SHA256 hash and to do any kind of comparing
/// </summary>
public partial class SHA256(ILogger logger)
{
    public static readonly SHA256 Instance = new SHA256(NullLogger.Instance);
    
    private static readonly Regex Sha256Regex = MyRegex();

    /// <summary>
    /// Checks the output of a <see cref="Stream"/> to a SHA256 hash that is expected
    /// </summary>
    /// <param name="stream"><see cref="Stream"/> to check against</param>
    /// <param name="expectedSHA256">Hash that we are expecting</param>
    /// <returns>If the <see cref="Stream"/> outputs the same hash as we are expecting</returns>
    public bool CheckSHA256(Stream stream, string expectedSHA256)
    {
        stream.Seek(0, SeekOrigin.Begin);
        var hash = CreateSHA256Hash(stream);
        return hash == expectedSHA256;
    }

    /// <summary>
    /// Checks a <see cref="byte"/>[] to a SHA256 hash that is expected
    /// </summary>
    /// <param name="byteArray"><see cref="byte"/>[] to check against</param>
    /// <param name="expectedSHA256">Hash that we are expecting</param>
    /// <returns>If the <see cref="byte"/>[] outputs the same hash as we are expecting</returns>
    public bool CheckSHA256(byte[] byteArray, string expectedSHA256)
    {
        if (IsValidSHA256(expectedSHA256))
        {
            var sameHash = CreateSHA256Hash(byteArray) == expectedSHA256;
            logger.LogInformation("Do we have the expected SHA256 hash?: {SameHash}", sameHash);
            return sameHash;
        }

        logger.LogWarning("We been given an invalid hash, can't check");
        return false;
    }

    /// <summary>
    /// Creates a SHA256 hash from a <see cref="Stream"/>
    /// </summary>
    /// <param name="stream"><see cref="Stream"/> to create hash for</param>
    /// <returns>SHA256 hash</returns>
    public string CreateSHA256Hash(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        return CreateSHA256Hash(sha256.ComputeHash(stream));
    }

    /// <summary>
    /// Creates a SHA256 hash from a <see cref="byte"/>[]
    /// </summary>
    /// <param name="bytes"><see cref="byte"/>[] to use for creating SHA256 hash</param>
    public string CreateSHA256Hash(byte[] bytes)
    {
        string result = string.Empty;
        foreach (var b in bytes)
        {
            result += b.ToString("X2");
        }
        return result;
    }

    /// <summary>
    /// Gets if this string is a valid SHA256 hash  
    /// </summary>
    /// <param name="s">string to check</param>
    public bool IsValidSHA256(string s) => !string.IsNullOrWhiteSpace(s) && Sha256Regex.IsMatch(s);

    [GeneratedRegex("^[a-fA-F0-9]{64}$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}