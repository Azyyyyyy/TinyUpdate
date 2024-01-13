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
    /// <param name="stream">Stream of that file</param>
    /// <param name="hasher">Hasher to use for grabbing checksums</param>
    /// <returns>Hash and filesize that is expected</returns>
    public static async Task<(string? hash, long filesize)> GetShasumDetails(this Stream stream, IHasher hasher)
    {
        //We expect the information to be contained in the following way:
        //<hash> <filesize>
        
        using var textStream = new StreamReader(stream);
        var text = await textStream.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(text))
        {
            return (null, -1);
        }

        //TODO: Make a Span splitter
        //Grab what we need, checking that it's what we expect
        var textSplit = text.Split(' ');
        var hash = textSplit[0];

        if (textSplit.Length != 2 ||
            string.IsNullOrWhiteSpace(hash) ||
            !hasher.IsValidHash(hash) ||
            !long.TryParse(textSplit[1], out var filesize))
        {
            return (null, -1);
        }

        return (hash, filesize);
    }
}