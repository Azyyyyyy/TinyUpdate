using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace TinyUpdate.Core.Utils
{
    /// <summary>
    /// Easy access to processing Streams to get a SHA1 hash and to do any kind of comparing
    /// </summary>
    public static class SHA1
    {
        /// <summary>
        /// Checks the output of a <see cref="Stream"/> to a SHA1 hash that is expected
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to check against</param>
        /// <param name="expectedSHA1">Hash that we are expecting</param>
        /// <returns>If the <see cref="Stream"/> outputs the same hash as we are expecting</returns>
        public static bool CheckSHA1(Stream stream, string expectedSHA1)
        {
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
        public static bool CheckSHA1(IEnumerable<byte> byteArray, string expectedSHA1) =>
            CreateSHA1Hash(byteArray) == expectedSHA1;

        /// <summary>
        /// Creates a SHA1 hash from a <see cref="Stream"/>
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to create hash for</param>
        /// <returns>SHA1 hash</returns>
        public static string CreateSHA1Hash(Stream stream)
        {
            using SHA1Managed sha1 = new();
            return CreateSHA1Hash(sha1.ComputeHash(stream));
        }

        /// <summary>
        /// Creates a SHA1 hash from a <see cref="byte"/>[]
        /// </summary>
        /// <param name="byteArray"><see cref="byte"/>[] to create hash for</param>
        /// <returns>SHA1 hash</returns>
        public static string CreateSHA1Hash(IEnumerable<byte> byteArray) =>
            byteArray.Aggregate("", (current, b) => current + b.ToString("X2"));
    }
}