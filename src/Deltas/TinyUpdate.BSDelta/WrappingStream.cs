namespace TinyUpdate.BSDelta;

//TODO: Add more modern overrides to this
/// <summary>
///     A <see cref="Stream" /> that wraps another stream. One major feature of <see cref="WrappingStream" /> is that it
///     does not dispose the
///     underlying stream when it is disposed if Ownership.None is used; this is useful when using classes such as
///     <see cref="BinaryReader" /> and
///     <see cref="System.Security.Cryptography.CryptoStream" /> that take ownership of the stream passed to their
///     constructors.
/// </summary>
/// <remarks>
///     See
///     <a href="http://code.logos.com/blog/2009/05/wrappingstream_implementation.html">WrappingStream Implementation</a>.
/// </remarks>
internal class WrappingStream : Stream
{
    private readonly Ownership _mOwnership;

    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WrappingStream" /> class.
    /// </summary>
    /// <param name="streamBase">The wrapped stream.</param>
    /// <param name="ownership">Use Owns if the wrapped stream should be disposed when this stream is disposed.</param>
    public WrappingStream(Stream streamBase, Ownership ownership)
    {
        ArgumentNullException.ThrowIfNull(streamBase);
        
        WrappedStream = streamBase;
        _mOwnership = ownership;
    }

    /// <summary>
    ///     Gets a value indicating whether the current stream supports reading.
    /// </summary>
    /// <returns><c>true</c> if the stream supports reading; otherwise, <c>false</c>.</returns>
    public override bool CanRead => WrappedStream.CanRead;

    /// <summary>
    ///     Gets a value indicating whether the current stream supports seeking.
    /// </summary>
    /// <returns><c>true</c> if the stream supports seeking; otherwise, <c>false</c>.</returns>
    public override bool CanSeek => WrappedStream.CanSeek;

    /// <summary>
    ///     Gets a value indicating whether the current stream supports writing.
    /// </summary>
    /// <returns><c>true</c> if the stream supports writing; otherwise, <c>false</c>.</returns>
    public override bool CanWrite => WrappedStream.CanWrite;

    /// <summary>
    ///     Gets the length in bytes of the stream.
    /// </summary>
    public override long Length
    {
        get
        {
            ThrowIfDisposed();
            return WrappedStream.Length;
        }
    }

    /// <summary>
    ///     Gets or sets the position within the current stream.
    /// </summary>
    public override long Position
    {
        get
        {
            ThrowIfDisposed();
            return WrappedStream.Position;
        }
        set
        {
            ThrowIfDisposed();
            WrappedStream.Position = value;
        }
    }

    /// <summary>
    ///     Gets the wrapped stream.
    /// </summary>
    /// <value>The wrapped stream.</value>
    protected Stream WrappedStream { get; }

    /// <summary>
    ///     Begins an asynchronous read operation.
    /// </summary>
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback,
        object? state)
    {
        ThrowIfDisposed();
        return WrappedStream.BeginRead(buffer, offset, count, callback, state);
    }

    /// <summary>
    ///     Begins an asynchronous write operation.
    /// </summary>
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback,
        object? state)
    {
        ThrowIfDisposed();
        return WrappedStream.BeginWrite(buffer, offset, count, callback, state);
    }

    /// <summary>
    ///     Waits for the pending asynchronous read to complete.
    /// </summary>
    public override int EndRead(IAsyncResult asyncResult)
    {
        ThrowIfDisposed();
        return WrappedStream.EndRead(asyncResult);
    }

    /// <summary>
    ///     Ends an asynchronous write operation.
    /// </summary>
    public override void EndWrite(IAsyncResult asyncResult)
    {
        ThrowIfDisposed();
        WrappedStream.EndWrite(asyncResult);
    }

    /// <summary>
    ///     Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
    /// </summary>
    public override void Flush()
    {
        ThrowIfDisposed();
        WrappedStream.Flush();
    }

    /// <summary>
    ///     Reads a sequence of bytes from the current stream and advances the position
    ///     within the stream by the number of bytes read.
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        return WrappedStream.Read(buffer, offset, count);
    }

    /// <summary>
    ///     Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end
    ///     of the stream.
    /// </summary>
    public override int ReadByte()
    {
        ThrowIfDisposed();
        return WrappedStream.ReadByte();
    }

    /// <summary>
    ///     Sets the position within the current stream.
    /// </summary>
    /// <param name="offset">A byte offset relative to the <paramref name="origin" /> parameter.</param>
    /// <param name="origin">
    ///     A value of type <see cref="T:System.IO.SeekOrigin" /> indicating the reference point used to
    ///     obtain the new position.
    /// </param>
    /// <returns>The new position within the current stream.</returns>
    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        return WrappedStream.Seek(offset, origin);
    }

    /// <summary>
    ///     Sets the length of the current stream.
    /// </summary>
    /// <param name="value">The desired length of the current stream in bytes.</param>
    public override void SetLength(long value)
    {
        ThrowIfDisposed();
        WrappedStream.SetLength(value);
    }

    /// <summary>
    ///     Writes a sequence of bytes to the current stream and advances the current position
    ///     within this stream by the number of bytes written.
    /// </summary>
    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        WrappedStream.Write(buffer, offset, count);
    }

    /// <summary>
    ///     Writes a byte to the current position in the stream and advances the position within the stream by one byte.
    /// </summary>
    public override void WriteByte(byte value)
    {
        ThrowIfDisposed();
        WrappedStream.WriteByte(value);
    }

    /// <summary>
    ///     Releases the unmanaged resources used by the <see cref="WrappingStream" /> and optionally releases the managed
    ///     resources.
    /// </summary>
    /// <param name="disposing">
    ///     true to release both managed and unmanaged resources; false to release only unmanaged
    ///     resources.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        // doesn't close the base stream, but just prevents access to it through this WrappingStream
        if (!disposing) return;

        if (_mOwnership == Ownership.Owns) WrappedStream.Dispose();

        _disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>
    ///     Throws if the stream is disposed
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, GetType().Name);
}

/// <summary>
///     Indicates whether an object takes ownership of an item.
/// </summary>
internal enum Ownership
{
    /// <summary>
    ///     The object does not own this item.
    /// </summary>
    None,

    /// <summary>
    ///     The object owns this item, and is responsible for releasing it.
    /// </summary>
    Owns
}