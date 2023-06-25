using System;

namespace TinyUpdate.Core.Extensions;

public static class DateTimeExt
{
    /// <summary>
    /// Makes this <see cref="DateTime"/> into a string that can be used in a filename
    /// </summary>
    /// <param name="time">The time to make into a filename</param>
    public static string ToFileName(this DateTime time) => time.ToString("d-M-yyyy_h-mm-ss");
}