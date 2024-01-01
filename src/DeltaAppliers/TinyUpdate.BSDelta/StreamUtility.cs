namespace TinyUpdate.BSDelta;

/// <summary>
///     Provides helper methods for working with <see cref="Stream" />.
/// </summary>
internal static class StreamUtility
{
    /// <summary>
    ///     Reads exactly <paramref name="count" /> bytes from <paramref name="stream" />.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="count">The count of bytes to read.</param>
    /// <returns>A new byte array containing the data read from the stream.</returns>
    public static byte[] ReadExactly(this Stream stream, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var buffer = new byte[count];
        stream.ReadExactly(buffer, 0, count);
        return buffer;
    }
}