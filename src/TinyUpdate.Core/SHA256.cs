using System.Text.RegularExpressions;
using TinyUpdate.Core.Abstract;

namespace TinyUpdate.Core;

/// <summary>
/// Easy access to processing Streams into a SHA256 hash and comparing SHA256 hashes
/// </summary>
public partial class SHA256 : IHasher
{
    private const string EmptyHash = "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855";
    private static readonly Regex Sha256Regex = MyRegex();

    public static readonly SHA256 Instance = new SHA256();

    public bool CompareHash(Stream stream, string expectedHash)
    {
        if (IsValidHash(expectedHash))
        {
            var hash = HashData(stream);
            return hash == expectedHash;
        }

        return false;
    }

    public bool CompareHash(byte[] byteArray, string expectedHash)
    {
        if (IsValidHash(expectedHash))
        {
            var hash = HashData(byteArray, true);
            return hash == expectedHash;
        }

        return false;
    }

    public string HashData(Stream stream)
    {
        //If we got nothing then return this, this will always be calculated by below
        if (stream is { CanSeek: true, Length: 0 })
        {
            return EmptyHash;
        }
        
        var dataHashed = System.Security.Cryptography.SHA256.HashData(stream);
        return HashData(dataHashed, false);
    }

    public string HashData(byte[] bytes) => HashData(bytes, true);

    public bool IsValidHash(string hash) => !string.IsNullOrWhiteSpace(hash) && Sha256Regex.IsMatch(hash);

    [GeneratedRegex("^[a-fA-F0-9]{64}$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
    
    private static string HashData(byte[] bytes, bool processBytes)
    {
        if (processBytes)
        {
            //If we got nothing then return this, this will always be calculated by below
            if (bytes.Length == 0)
            {
                return EmptyHash;
            }
            
            using var memStream = new MemoryStream(bytes);
            bytes = System.Security.Cryptography.SHA256.HashData(memStream);
        }

        var resultArray = new Span<char>(new char[64]);
        var charsWritten = 0;
        
        foreach (var @byte in bytes)
        {
            @byte.TryFormat(resultArray[charsWritten..], out var written, "X2");
            charsWritten += written;
        }
        return resultArray.ToString();
    }
}