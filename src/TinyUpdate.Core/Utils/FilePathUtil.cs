using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Core.Utils;

/// <summary>
/// Functions to assist in making sure that what is passed for files are valid
/// </summary>
public static class FilePathUtil
{
    private static readonly ILogger Logger = LogManager.CreateLogger(nameof(FilePathUtil));
    private static readonly char[] FileNameInvalidChars;
    private static readonly char[] PathInvalidChars;

    static FilePathUtil()
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        FileNameInvalidChars = new char[invalidChars.LongLength];
        for (var i = 0; i < invalidChars.LongLength; i++)
        {
            FileNameInvalidChars[i] = invalidChars[i];
        }

        invalidChars = Path.GetInvalidPathChars();
        PathInvalidChars = new char[invalidChars.LongLength];
        for (var i = 0; i < invalidChars.LongLength; i++)
        {
            PathInvalidChars[i] = invalidChars[i];
        }
    }

    /// <summary>
    /// Gets if the string contains any char that is not allowed in a file path
    /// </summary>
    /// <param name="s">File name to check</param>
    /// <param name="invalidChar"><see cref="char"/> that is invalid</param>
    public static bool IsValidForFileName(this string s, out char? invalidChar) =>
        CheckValidation(FileNameInvalidChars, s, out invalidChar);

    /// <summary>
    /// Gets if the string contains any char that is not allowed in a file name
    /// </summary>
    /// <param name="s">File path to check</param>
    /// <param name="invalidChar"><see cref="char"/> that is invalid</param>
    public static bool IsValidForFilePath(this string s, out char? invalidChar) =>
        CheckValidation(PathInvalidChars, s, out invalidChar);

    /// <summary>
    /// Checks string for anything that it shouldn't be in it
    /// </summary>
    /// <param name="chars"><see cref="char"/>[] that shouldn't be in <see cref="s"/></param>
    /// <param name="s">string to check</param>
    /// <param name="invalidChar"><see cref="char"/> that was in <see cref="s"/> but shouldn't be</param>
    private static bool CheckValidation(IEnumerable<char> chars, string s, out char? invalidChar)
    {
        Logger.Log(Level.Trace, $"Checking {s}");
        invalidChar = null;

        //Check that the string even has anything
        if (string.IsNullOrWhiteSpace(s))
        {
            Logger.Log(Level.Trace,$"{s} contains nothing");
            return false;
        }

        //Check the chars
        foreach (var sChar in s.Where(chars.Contains))
        {
            invalidChar = sChar;
            Logger.Log(Level.Trace,$"{s} is not usable");
            return false;
        }

        Logger.Log(Level.Trace,$"{s} is usable");
        return true;
    }
}