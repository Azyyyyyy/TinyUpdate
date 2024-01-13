namespace TinyUpdate.Delta.BSDiff;

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
    // The wrapped stream.
    private readonly Stream _wrappedStream;
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
        
        _wrappedStream = streamBase;
        _mOwnership = ownership;
    }

    public override bool CanRead
    {
        get
        {
            ThrowIfDisposed();
            return _wrappedStream.CanRead;
        }
    }

    public override bool CanWrite
    {
        get
        {
            ThrowIfDisposed();
            return _wrappedStream.CanWrite;
        }
    }

    public override bool CanSeek
    {
        get
        {
            ThrowIfDisposed();
            return _wrappedStream.CanSeek;
        }
    }

    public override bool CanTimeout
    {
        get
        {
            ThrowIfDisposed();
            return _wrappedStream.CanTimeout;
        }
    }

    public override long Length
    {
        get
        {
            ThrowIfDisposed();
            return _wrappedStream.Length;
        }
    }

    public override long Position
    {
        get
        {
            ThrowIfDisposed();
            return _wrappedStream.Position;
        }
        set
        {
            ThrowIfDisposed();
            _wrappedStream.Position = value;
        }
    }

    public override int ReadTimeout
    {
        get
        {
            ThrowIfDisposed();
            return _wrappedStream.ReadTimeout;
        }
        set
        {
            ThrowIfDisposed();
            _wrappedStream.ReadTimeout = value;
        }
    }

    public override int WriteTimeout
    {
        get
        {
            ThrowIfDisposed();
            return _wrappedStream.WriteTimeout;
        }
        set
        {
            ThrowIfDisposed();
            _wrappedStream.WriteTimeout = value;
        }
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        ThrowIfDisposed();
        _wrappedStream.CopyTo(destination, bufferSize);
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return _wrappedStream.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    public override void Close()
    {
        ThrowIfDisposed();
        base.Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;

        // doesn't close the base stream, but just prevents access to it through this WrappingStream
        if (_mOwnership == Ownership.Owns) 
            _wrappedStream.Dispose();

        _disposed = true;
    }
    
    public override ValueTask DisposeAsync()
    {
        var returnValue = ValueTask.CompletedTask;

        // doesn't close the base stream, but just prevents access to it through this WrappingStream
        if (_mOwnership == Ownership.Owns)
            returnValue = _wrappedStream.DisposeAsync();

        _disposed = true;
        return returnValue;
    }
    
    public override void Flush()
    {
        ThrowIfDisposed();
        _wrappedStream.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return _wrappedStream.FlushAsync(cancellationToken);
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback,
        object? state)
    {
        ThrowIfDisposed();
        return _wrappedStream.BeginRead(buffer, offset, count, callback, state);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        ThrowIfDisposed();
        return _wrappedStream.EndRead(asyncResult);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return _wrappedStream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        ThrowIfDisposed();
        return _wrappedStream.ReadAsync(buffer, cancellationToken);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback,
        object? state)
    {
        ThrowIfDisposed();
        return _wrappedStream.BeginWrite(buffer, offset, count, callback, state);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        ThrowIfDisposed();
        _wrappedStream.EndWrite(asyncResult);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return _wrappedStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        ThrowIfDisposed();
        return _wrappedStream.WriteAsync(buffer, cancellationToken);
    }
    
    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        return _wrappedStream.Seek(offset, origin);
    }
    
    public override void SetLength(long value)
    {
        ThrowIfDisposed();
        _wrappedStream.SetLength(value);
    }
    
    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        return _wrappedStream.Read(buffer, offset, count);
    }

    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();
        return _wrappedStream.Read(buffer);
    }
    
    public override int ReadByte()
    {
        ThrowIfDisposed();
        return _wrappedStream.ReadByte();
    }
    
    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        _wrappedStream.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        _wrappedStream.Write(buffer);
    }

    public override void WriteByte(byte value)
    {
        ThrowIfDisposed();
        _wrappedStream.WriteByte(value);
    }

    /// <summary>
    ///     Throws if the stream is disposed
    /// </summary>
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