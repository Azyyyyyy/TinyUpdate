using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using TinyUpdate.Core.Logger;

namespace TinyUpdate.Core.Utils
{
    /// <summary>
    /// Easy access to processing Streams to get a SHA1 hash and to do any kind of comparing
    /// </summary>
    public static class SHA1Util
    {
        private static readonly ILogging Logger = Logging.CreateLogger("SHA1");
        private static readonly Regex SHA1Regex = new("^[a-fA-F0-9]{40}$");
        
        /// <summary>
        /// Checks the output of a <see cref="Stream"/> to a SHA1 hash that is expected
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to check against</param>
        /// <param name="expectedSHA1">Hash that we are expecting</param>
        /// <returns>If the <see cref="Stream"/> outputs the same hash as we are expecting</returns>
        public static bool CheckSHA1(Stream stream, string expectedSHA1)
        {
            stream.Seek(0, SeekOrigin.Begin);
            //Make a byte[] that can be used for hashing
            using SHA1Managed sha1 = new();
            return CheckSHA1(sha1.ComputeHash(stream), expectedSHA1);
        }

        /// <summary>
        /// Checks a <see cref="byte"/>[] to a SHA1 hash that is expected
        /// </summary>
        /// <param name="byteArray"><see cref="byte"/>[] to check against</param>
        /// <param name="expectedSHA1">Hash that we are expecting</param>
        /// <returns>If the <see cref="byte"/>[] outputs the same hash as we are expecting</returns>
        public static bool CheckSHA1(byte[] byteArray, string expectedSHA1)
        {
            if (IsValidSHA1(expectedSHA1))
            {
                var sameHash = CreateSHA1Hash(byteArray) == expectedSHA1;
                Logger.Information("Do we have the expected SHA1 hash?: {0}", sameHash);
                return sameHash;
            }
            Logger.Warning("We been given an invalid hash, can't check");
            return false;
        }

        /// <summary>
        /// Creates a SHA1 hash from a <see cref="Stream"/>
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to create hash for</param>
        /// <returns>SHA1 hash</returns>
        public static string CreateSHA1Hash(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            using SHA1Managed sha1 = new();
            return CreateSHA1Hash(sha1.ComputeHash(stream));
        }
        
        /// <summary>
        /// Creates a SHA1 hash from a <see cref="byte"/>[]
        /// </summary>
        /// <param name="bytes"><see cref="byte"/>[] to use for creating SHA1 hash</param>
        public static string CreateSHA1Hash(byte[] bytes) => 
            bytes.Aggregate("", (current, b) => current + b.ToString("X2"));
        
        /// <summary>
        /// Gets if this string is a valid sha1 hash  
        /// </summary>
        /// <param name="s">string to check</param>
        public static bool IsValidSHA1(string s) => !string.IsNullOrWhiteSpace(s) && SHA1Regex.IsMatch(s);
    }
}