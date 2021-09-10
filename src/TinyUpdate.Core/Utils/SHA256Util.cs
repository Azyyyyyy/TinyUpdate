using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using TinyUpdate.Core.Logging;
// ReSharper disable MemberCanBePrivate.Global

namespace TinyUpdate.Core.Utils
{
    /// <summary>
    /// Easy access to processing Streams to get a SHA256 hash and to do any kind of comparing
    /// </summary>
    public static class SHA256Util
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(SHA256Util));
        private static readonly Regex SHA256Regex = new("^[a-fA-F0-9]{64}$", RegexOptions.Compiled);

        /// <summary>
        /// Checks the output of a <see cref="Stream"/> to a SHA256 hash that is expected
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to check against</param>
        /// <param name="expectedSHA256">Hash that we are expecting</param>
        /// <returns>If the <see cref="Stream"/> outputs the same hash as we are expecting</returns>
        public static bool CheckSHA256(Stream stream, string expectedSHA256)
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
        public static bool CheckSHA256(byte[] byteArray, string expectedSHA256)
        {
            if (IsValidSHA256(expectedSHA256))
            {
                var sameHash = CreateSHA256Hash(byteArray) == expectedSHA256;
                Logger.Information("Do we have the expected SHA256 hash?: {0}", sameHash);
                return sameHash;
            }

            Logger.Warning("We been given an invalid hash, can't check");
            return false;
        }

        /// <summary>
        /// Creates a SHA256 hash from a <see cref="Stream"/>
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to create hash for</param>
        /// <returns>SHA256 hash</returns>
        public static string CreateSHA256Hash(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            using SHA256 sha256 = SHA256.Create();
            return CreateSHA256Hash(sha256.ComputeHash(stream));
        }

        /// <summary>
        /// Creates a SHA256 hash from a <see cref="byte"/>[]
        /// </summary>
        /// <param name="bytes"><see cref="byte"/>[] to use for creating SHA256 hash</param>
        public static string CreateSHA256Hash(byte[] bytes)
        {
            string result = string.Empty;
            foreach (var b in bytes)
            {
                result = result + b.ToString("X2");
            }
            return result;
        }

        /// <summary>
        /// Gets if this string is a valid SHA256 hash  
        /// </summary>
        /// <param name="s">string to check</param>
        public static bool IsValidSHA256(string s) => !string.IsNullOrWhiteSpace(s) && SHA256Regex.IsMatch(s);
    }
}