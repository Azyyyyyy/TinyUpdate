using TinyUpdate.Core.Abstract;

namespace TinyUpdate.TUUP;

/// <summary>
/// Extensions to make grabbing data from <see cref="Stream"/>'s easier
/// </summary>
public static class StreamExt
{
    /// <summary>
    /// Gets the hash and filesize from a file that contains data about a file we need to use for updating
    /// </summary>
    /// <param name="fileStream">Stream of that file</param>
    /// <param name="hasher">Hasher to use for grabbing checksums</param>
    /// <returns>Hash and filesize that is expected</returns>
    public static async Task<(string? hash, long filesize)> GetShasumDetails(this Stream fileStream, IHasher hasher)
    {
        //Grab the text from the file
        using var textStream = new StreamReader(fileStream);
        var text = await textStream.ReadToEndAsync();

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
            !hasher.IsValidHash(hash) ||
            !long.TryParse(textS[1], out var filesize))
        {
            return (null, -1);
        }

        return (hash, filesize);
    }
}