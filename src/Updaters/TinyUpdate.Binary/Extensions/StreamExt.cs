using System.IO;
using System.Threading.Tasks;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Binary.Extensions
{
    /// <summary>
    /// Extensions to make grabbing data from <see cref="Stream"/>'s easier
    /// </summary>
    public static class StreamExt
    {
        /// <summary>
        /// Gets the hash and filesize from a file that contains data about a file we need to use for updating
        /// </summary>
        /// <param name="fileStream">Stream of that file</param>
        /// <returns>SHA256 hash and filesize that is expected</returns>
        public static async Task<(string? sha256Hash, long filesize)> GetShasumDetails(this Stream fileStream)
        {
            //Grab the text from the file
            var textStream = new StreamReader(fileStream);
            var text = await textStream.ReadToEndAsync();

            textStream.Dispose();

            //Return nothing if we don't have anything
            if (string.IsNullOrWhiteSpace(text))
            {
                return (null, -1);
            }

            //Grab what we need, checking that it's what we expect
            var textS = text.Split(' ');
            var hash = textS[0];
            if (textS.Length != 2 ||
                string.IsNullOrWhiteSpace(hash) ||
                !SHA256Util.IsValidSHA256(hash) ||
                !long.TryParse(textS[1], out var filesize))
            {
                return (null, -1);
            }

            return (hash, filesize);
        }
    }
}