using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Core
{
    public class ProgressStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly Action<int>? _readAction;
        private readonly Action<int>? _writeAction;
        public ProgressStream(Stream innerStream, Action<int>? readAction = null, Action<int>? writeAction = null)
        {
            _innerStream = innerStream;
            _readAction = readAction;
            _writeAction = writeAction;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _innerStream.Read(buffer, offset, count);
            _readAction?.Invoke(read);
            return read;
        }

        public override int ReadByte()
        {
            var byteRead = _innerStream.ReadByte();
            _readAction?.Invoke(byteRead == -1 ? 0 : 1);
            return byteRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                _readAction?.Invoke(read);
            }
            return read;
        }
        
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return _innerStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            var read = _innerStream.EndRead(asyncResult);
            _readAction?.Invoke(read);
            return read;
        }
        
        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
            _writeAction?.Invoke(count);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return _innerStream.BeginWrite(buffer, offset, count, ar =>
            {
                callback?.Invoke(ar);
                _writeAction?.Invoke(count);
            }, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _innerStream.EndWrite(asyncResult);
        }
        
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                _writeAction?.Invoke(count);
            }
        }

        public override void WriteByte(byte value)
        {
            _innerStream.WriteByte(value);
            _writeAction?.Invoke(1);
        }

        private readonly ILogging _logger = LoggingCreator.CreateLogger(nameof(ProgressStream));
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            long amountToWrite = -1;
            if (CanSeek && destination.CanSeek)
            {
                amountToWrite = _innerStream.Length - _innerStream.Position;
            }

            if (amountToWrite == -1)
            {
                _logger.Warning("We can't give a report for CopyToAsync due to one of the streams not supporting Seeking");
                await _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
                return;
            }

            var buf = new byte[bufferSize];
            for (int i = 0; i < amountToWrite; i += bufferSize)
            {
                var readCount = await _innerStream.ReadAsync(buf, 0, bufferSize, cancellationToken);
                await destination.WriteAsync(buf, 0, readCount, cancellationToken);
                _readAction?.Invoke(readCount);
            }
        }
        
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _innerStream.FlushAsync(cancellationToken);
        }
        public override void Flush()
        {
            _innerStream.Flush();
        }
        public override void Close() => _innerStream.Close();

        public override long Seek(long offset, SeekOrigin origin)
        {
            var read = _innerStream.Seek(offset, origin);
            return read;
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }
        
#if NET5_0_OR_GREATER
        [Obsolete("This Remoting API is not supported and throws PlatformNotSupportedException.", DiagnosticId = "SYSLIB0010", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
#endif
        public override object InitializeLifetimeService() => _innerStream.InitializeLifetimeService()!;
        public override string? ToString() => _innerStream.ToString();
        public override bool Equals(object? obj) => _innerStream.Equals(obj);
        public override int GetHashCode() => _innerStream.GetHashCode();
        
        public override bool CanTimeout => _innerStream.CanTimeout;
        public override int ReadTimeout => _innerStream.ReadTimeout;
        public override int WriteTimeout => _innerStream.WriteTimeout;
        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;
        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }
    }
}