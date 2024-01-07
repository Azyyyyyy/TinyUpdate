using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TinyUpdate.Core.Abstract;

namespace TinyUpdate.Core;

/// <summary>
/// Easy access to processing Streams into a SHA256 hash and comparing SHA256 hashes
/// </summary>
public partial class SHA256(ILogger logger) : IHasher
{
    public static readonly SHA256 Instance = new SHA256(NullLogger.Instance);
    
    private static readonly Regex Sha256Regex = MyRegex();

    public bool CheckHash(Stream stream, string expectedHash)
    {
        stream.Seek(0, SeekOrigin.Begin);
        var hash = CreateHash(stream);
        return hash == expectedHash;
    }

    public bool CheckHash(byte[] byteArray, string expectedHash)
    {
        if (IsValidHash(expectedHash))
        {
            var sameHash = CreateHash(byteArray) == expectedHash;
            logger.LogInformation("Do we have the expected SHA256 hash?: {SameHash}", sameHash);
            return sameHash;
        }

        logger.LogWarning("We been given an invalid hash, can't check");
        return false;
    }

    public string CreateHash(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        return CreateHash(sha256.ComputeHash(stream));
    }

    public string CreateHash(byte[] bytes)
    {
        string result = string.Empty;
        foreach (var b in bytes)
        {
            result += b.ToString("X2");
        }
        return result;
    }

    public bool IsValidHash(string hash) => !string.IsNullOrWhiteSpace(hash) && Sha256Regex.IsMatch(hash);

    [GeneratedRegex("^[a-fA-F0-9]{64}$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}