using System;
using System.IO;
using System.Threading.Tasks;
using SharpCompress.Compressors;
using SharpCompress.Compressors.BZip2;
using TinyUpdate.Core.Logging;

// Squirrel.Bsdiff: Adapted from https://github.com/LogosBible/bsdiff.net/blob/master/src/bsdiff/BinaryPatchUtility.cs
// TinyUpdate: Adapted from https://github.com/Squirrel/Squirrel.Windows/blob/develop/src/Squirrel/BinaryPatchUtility.cs
namespace TinyUpdate.Binary
{
    /*
    Permission is hereby granted,  free of charge,  to any person obtaining a
    copy of this software and associated documentation files (the "Software"),
    to deal in the Software without restriction, including without limitation
    the rights to  use, copy, modify, merge, publish, distribute, sublicense,
    and/or sell copies of the Software, and to permit persons to whom the
    Software is furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
    FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
    DEALINGS IN THE SOFTWARE.

    The original bsdiff.c source code (http://www.daemonology.net/bsdiff/) is
    distributed under the following license:

    Copyright 2003-2005 Colin Percival
    All rights reserved

    Redistribution and use in source and binary forms, with or without
    modification, are permitted providing that the following conditions
    are met:
    1. Redistributions of source code must retain the above copyright
        notice, this list of conditions and the following disclaimer.
    2. Redistributions in binary form must reproduce the above copyright
        notice, this list of conditions and the following disclaimer in the
        documentation and/or other materials provided with the distribution.

    THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
    IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
    WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
    ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY
    DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
    DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
    OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
    HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
    STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
    IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
    POSSIBILITY OF SUCH DAMAGE.
    */
    internal static class BinaryPatchUtility
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(BinaryPatchUtility));

        /// <summary>
        /// Creates a binary patch (in <a href="http://www.daemonology.net/bsdiff/">bsdiff</a> format) that can be used
        /// (by <see cref="Apply"/>) to transform <paramref name="oldData"/> into <paramref name="newData"/>.
        /// </summary>
        /// <param name="oldData">The original binary data.</param>
        /// <param name="newData">The new binary data.</param>
        /// <param name="output">A <see cref="Stream"/> to which the patch will be written.</param>
        /// <param name="progress">Reports back progress making delta file</param>
        public static bool Create(byte[] oldData, byte[] newData, Stream output, Action<decimal>? progress = null)
        {
            // check arguments
            if (!output.CanSeek)
            {
                Logger.Error("Output stream must be seekable");
                return false;
            }

            if (!output.CanWrite)
            {
                Logger.Error("Output stream must be writable");
                return false;
            }

            /* Header is
                0   8    "BSDIFF40"
                8   8   length of bzip2ed ctrl block
                16  8   length of bzip2ed diff block
                24  8   length of new file */
            /* File is
                0   32  Header
                32  ??  Bzip2ed ctrl block
                ??  ??  Bzip2ed diff block
                ??  ??  Bzip2ed extra block */
            byte[] header = new byte[CHeaderSize];
            WriteInt64(CFileSignature, header, 0); // "BSDIFF40"
            WriteInt64(0, header, 8);
            WriteInt64(0, header, 16);
            WriteInt64(newData.Length, header, 24);

            long startPosition = output.Position;
            output.Write(header, 0, header.Length);

            var I = SuffixSort(oldData);
            progress?.Invoke(0.5m);

            byte[] db = new byte[newData.Length];
            byte[] eb = new byte[newData.Length];

            int dbLen = 0;
            int ebLen = 0;

            using (WrappingStream wrappingStream = new(output, Ownership.None))
            using (BZip2Stream bz2Stream = new(wrappingStream, CompressionMode.Compress, false))
            {
                // compute the differences, writing ctrl as we go
                int scan = 0;
                int pos = 0;
                int len = 0;
                int lastScan = 0;
                int lastPos = 0;
                int lastOffset = 0;
                while (scan < newData.Length)
                {
                    progress?.Invoke((((decimal) scan / newData.Length) / 2) + 0.5m);

                    int oldScore = 0;

                    for (int scsc = scan += len; scan < newData.Length; scan++)
                    {
                        len = Search(I, oldData, newData, scan, 0, oldData.Length, out pos);

                        for (; scsc < scan + len; scsc++)
                        {
                            if ((scsc + lastOffset < oldData.Length) && (oldData[scsc + lastOffset] == newData[scsc]))
                                oldScore++;
                        }

                        if ((len == oldScore && len != 0) || (len > oldScore + 8))
                            break;

                        if ((scan + lastOffset < oldData.Length) && (oldData[scan + lastOffset] == newData[scan]))
                            oldScore--;
                    }

                    if (len != oldScore || scan == newData.Length)
                    {
                        int s = 0;
                        int sf = 0;
                        int lenf = 0;
                        for (int i = 0; (lastScan + i < scan) && (lastPos + i < oldData.Length);)
                        {
                            if (oldData[lastPos + i] == newData[lastScan + i])
                                s++;
                            i++;
                            if (s * 2 - i > sf * 2 - lenf)
                            {
                                sf = s;
                                lenf = i;
                            }
                        }

                        int lenb = 0;
                        if (scan < newData.Length)
                        {
                            s = 0;
                            int sb = 0;
                            for (int i = 1; (scan >= lastScan + i) && (pos >= i); i++)
                            {
                                if (oldData[pos - i] == newData[scan - i])
                                    s++;
                                if (s * 2 - i > sb * 2 - lenb)
                                {
                                    sb = s;
                                    lenb = i;
                                }
                            }
                        }

                        if (lastScan + lenf > scan - lenb)
                        {
                            int overlap = (lastScan + lenf) - (scan - lenb);
                            s = 0;
                            int ss = 0;
                            int lens = 0;
                            for (int i = 0; i < overlap; i++)
                            {
                                if (newData[lastScan + lenf - overlap + i] == oldData[lastPos + lenf - overlap + i])
                                    s++;
                                if (newData[scan - lenb + i] == oldData[pos - lenb + i])
                                    s--;
                                if (s > ss)
                                {
                                    ss = s;
                                    lens = i + 1;
                                }
                            }

                            lenf += lens - overlap;
                            lenb -= lens;
                        }

                        for (int i = 0; i < lenf; i++)
                            db[dbLen + i] = (byte) (newData[lastScan + i] - oldData[lastPos + i]);
                        for (int i = 0; i < (scan - lenb) - (lastScan + lenf); i++)
                            eb[ebLen + i] = newData[lastScan + lenf + i];

                        dbLen += lenf;
                        ebLen += (scan - lenb) - (lastScan + lenf);

                        byte[] buf = new byte[8];
                        WriteInt64(lenf, buf, 0);
                        bz2Stream.Write(buf, 0, 8);

                        WriteInt64((scan - lenb) - (lastScan + lenf), buf, 0);
                        bz2Stream.Write(buf, 0, 8);

                        WriteInt64((pos - lenb) - (lastPos + lenf), buf, 0);
                        bz2Stream.Write(buf, 0, 8);

                        lastScan = scan - lenb;
                        lastPos = pos - lenb;
                        lastOffset = pos - scan;
                    }
                }
            }

            // compute size of compressed ctrl data
            var controlEndPosition = output.Position;
            WriteInt64(controlEndPosition - startPosition - CHeaderSize, header, 8);

            // write compressed diff data
            if (dbLen > 0)
            {
                using WrappingStream wrappingStream = new(output, Ownership.None);
                using var bz2Stream = new BZip2Stream(wrappingStream, CompressionMode.Compress, false);
                bz2Stream.Write(db, 0, dbLen);
            }

            // compute size of compressed diff data
            long diffEndPosition = output.Position;
            WriteInt64(diffEndPosition - controlEndPosition, header, 16);

            // write compressed extra data
            if (ebLen > 0)
            {
                using WrappingStream wrappingStream = new(output, Ownership.None);
                using BZip2Stream bz2Stream = new(wrappingStream, CompressionMode.Compress, false);
                bz2Stream.Write(eb, 0, ebLen);
            }

            // seek to the beginning, write the header, then seek back to end
            long endPosition = output.Position;
            output.Position = startPosition;
            output.Write(header, 0, header.Length);
            output.Position = endPosition;

            return true;
        }

        /// <summary>
        /// Applies a binary patch (in <a href="http://www.daemonology.net/bsdiff/">bsdiff</a> format) to the data in
        /// <paramref name="input"/> and writes the results of patching to <paramref name="output"/>.
        /// </summary>
        /// <param name="input">A <see cref="Stream"/> containing the input data.</param>
        /// <param name="openPatchStream">A func that can open a <see cref="Stream"/> positioned at the start of the patch data.
        /// This stream must support reading and seeking, and <paramref name="openPatchStream"/> must allow multiple streams on
        /// the patch to be opened concurrently.</param>
        /// <param name="output">A <see cref="Stream"/> to which the patched data is written.</param>
        /// <param name="progress">Reports back progress</param>
        public static async Task<bool> Apply(Stream input, Func<Stream> openPatchStream, Stream output,
            Action<decimal>? progress)
        {
            /*
            File format:
                0   8   "BSDIFF40"
                8   8   X
                16  8   Y
                24  8   sizeof(newfile)
                32  X   bzip2(control block)
                32+X    Y   bzip2(diff block)
                32+X+Y  ??? bzip2(extra block)
            with control block a set of triples (x,y,z) meaning "add x bytes
            from oldfile to x bytes from the diff block; copy y bytes from the
            extra block; seek forwards in oldfile by z bytes".
            */
            // read header
            long controlLength, diffLength, newSize;
            using (Stream patchStream = openPatchStream())
            {
                // check patch stream capabilities
                if (!patchStream.CanRead)
                {
                    Logger.Error("Patch stream must be readable.");
                    return false;
                }

                if (!patchStream.CanSeek)
                {
                    Logger.Error("Patch stream must be seekable.");
                    return false;
                }

                byte[] header = patchStream.ReadExactly(CHeaderSize);

                // check for appropriate magic
                long signature = ReadInt64(header, 0);
                if (signature != CFileSignature)
                {
                    Logger.Error("Corrupt patch.");
                    return false;
                }

                // read lengths from header
                controlLength = ReadInt64(header, 8);
                diffLength = ReadInt64(header, 16);
                newSize = ReadInt64(header, 24);
                if (controlLength < 0 || diffLength < 0 || newSize < 0)
                {
                    Logger.Error("Corrupt patch.");
                    return false;
                }
            }

            // preallocate buffers for reading and writing
            const int cBufferSize = 1048576;
            byte[] newData = new byte[cBufferSize];
            byte[] oldData = new byte[cBufferSize];

            // prepare to read three parts of the patch in parallel
            using Stream compressedControlStream = openPatchStream();
            using Stream compressedDiffStream = openPatchStream();
            using Stream compressedExtraStream = openPatchStream();
            {
                // seek to the start of each part
                compressedControlStream.Seek(CHeaderSize, SeekOrigin.Current);
                compressedDiffStream.Seek(CHeaderSize + controlLength, SeekOrigin.Current);
                compressedExtraStream.Seek(CHeaderSize + controlLength + diffLength, SeekOrigin.Current);

                // decompress each part (to read it)
                using var controlStream = new BZip2Stream(compressedControlStream, CompressionMode.Decompress, false);
                using var diffStream = new BZip2Stream(compressedDiffStream, CompressionMode.Decompress, false);
                using var extraStream = new BZip2Stream(compressedExtraStream, CompressionMode.Decompress, false);
                var control = new long[3];
                var buffer = new byte[8];

                int oldPosition = 0;
                var newPosition = 0m;
                while (newPosition < newSize)
                {
                    if (newPosition != 0)
                    {
                        progress?.Invoke(newSize / newPosition);
                    }

                    // read control data
                    for (int i = 0; i < 3; i++)
                    {
                        controlStream.ReadExactly(buffer, 0, 8);
                        control[i] = ReadInt64(buffer, 0);
                    }

                    // sanity-check
                    if (newPosition + control[0] > newSize)
                    {
                        Logger.Error("Corrupt patch.");
                        return false;
                    }

                    // seek old file to the position that the new data is diffed against
                    input.Position = oldPosition;

                    int bytesToCopy = (int) control[0];
                    while (bytesToCopy > 0)
                    {
                        int actualBytesToCopy = Math.Min(bytesToCopy, cBufferSize);

                        // read diff string
                        diffStream.ReadExactly(newData, 0, actualBytesToCopy);

                        // add old data to diff string
                        int availableInputBytes = Math.Min(actualBytesToCopy, (int) (input.Length - input.Position));
                        input.ReadExactly(oldData, 0, availableInputBytes);

                        for (int index = 0; index < availableInputBytes; index++)
                            newData[index] += oldData[index];

                        await output.WriteAsync(newData, 0, actualBytesToCopy);

                        // adjust counters
                        newPosition += actualBytesToCopy;
                        oldPosition += actualBytesToCopy;
                        bytesToCopy -= actualBytesToCopy;
                    }

                    // sanity-check
                    if (newPosition + control[1] > newSize)
                    {
                        Logger.Error("Corrupt patch.");
                        return false;
                    }

                    // read extra string
                    bytesToCopy = (int) control[1];
                    while (bytesToCopy > 0)
                    {
                        int actualBytesToCopy = Math.Min(bytesToCopy, cBufferSize);

                        extraStream.ReadExactly(newData, 0, actualBytesToCopy);
                        await output.WriteAsync(newData, 0, actualBytesToCopy);

                        newPosition += actualBytesToCopy;
                        bytesToCopy -= actualBytesToCopy;
                    }

                    // adjust position
                    oldPosition = (int) (oldPosition + control[2]);
                }
            }

            return true;
        }

        private static int CompareBytes(byte[] left, int leftOffset, byte[] right, int rightOffset)
        {
            for (int index = 0; index < left.Length - leftOffset && index < right.Length - rightOffset; index++)
            {
                int diff = left[index + leftOffset] - right[index + rightOffset];
                if (diff != 0)
                    return diff;
            }

            return 0;
        }

        private static int MatchLength(byte[] oldData, int oldOffset, byte[] newData, int newOffset)
        {
            int i;
            for (i = 0; i < oldData.Length - oldOffset && i < newData.Length - newOffset; i++)
            {
                if (oldData[i + oldOffset] != newData[i + newOffset])
                    break;
            }

            return i;
        }

        private static int Search(int[] I, byte[] oldData, byte[] newData, int newOffset, int start, int end,
            out int pos)
        {
            while (true)
            {
                if (end - start < 2)
                {
                    int startLength = MatchLength(oldData, I[start], newData, newOffset);
                    int endLength = MatchLength(oldData, I[end], newData, newOffset);

                    if (startLength > endLength)
                    {
                        pos = I[start];
                        return startLength;
                    }

                    pos = I[end];
                    return endLength;
                }

                int midPoint = start + (end - start) / 2;
                if (CompareBytes(oldData, I[midPoint], newData, newOffset) < 0)
                {
                    start = midPoint;
                    continue;
                }

                end = midPoint;
            }
        }

        private static void Split(int[] I, int[] v, int start, int len, int h)
        {
            while (true)
            {
                if (len < 16)
                {
                    int j;
                    for (int k = start; k < start + len; k += j)
                    {
                        j = 1;
                        int x = v[I[k] + h];
                        for (int i = 1; k + i < start + len; i++)
                        {
                            if (v[I[k + i] + h] < x)
                            {
                                x = v[I[k + i] + h];
                                j = 0;
                            }

                            if (v[I[k + i] + h] == x)
                            {
                                Swap(ref I[k + j], ref I[k + i]);
                                j++;
                            }
                        }

                        for (int i = 0; i < j; i++) v[I[k + i]] = k + j - 1;
                        if (j == 1) I[k] = -1;
                    }
                }
                else
                {
                    int x = v[I[start + len / 2] + h];
                    int jj = 0;
                    int kk = 0;
                    for (int i2 = start; i2 < start + len; i2++)
                    {
                        if (v[I[i2] + h] < x) jj++;
                        if (v[I[i2] + h] == x) kk++;
                    }

                    jj += start;
                    kk += jj;

                    int i = start;
                    int j = 0;
                    int k = 0;
                    while (i < jj)
                    {
                        if (v[I[i] + h] < x)
                        {
                            i++;
                        }
                        else if (v[I[i] + h] == x)
                        {
                            Swap(ref I[i], ref I[jj + j]);
                            j++;
                        }
                        else
                        {
                            Swap(ref I[i], ref I[kk + k]);
                            k++;
                        }
                    }

                    while (jj + j < kk)
                    {
                        if (v[I[jj + j] + h] == x)
                        {
                            j++;
                        }
                        else
                        {
                            Swap(ref I[jj + j], ref I[kk + k]);
                            k++;
                        }
                    }

                    if (jj > start)
                    {
                        Split(I, v, start, jj - start, h);
                    }

                    for (i = 0; i < kk - jj; i++) v[I[jj + i]] = kk - 1;
                    if (jj == kk - 1) I[jj] = -1;

                    if (start + len > kk)
                    {
                        var start1 = start;
                        start = kk;
                        len = start1 + len - kk;
                        continue;
                    }
                }

                break;
            }
        }

        private static int[] SuffixSort(byte[] oldData)
        {
            int[] buckets = new int[256];

            foreach (byte oldByte in oldData)
                buckets[oldByte]++;
            for (int i = 1; i < 256; i++)
                buckets[i] += buckets[i - 1];
            for (int i = 255; i > 0; i--)
                buckets[i] = buckets[i - 1];
            buckets[0] = 0;

            int[] I = new int[oldData.Length + 1];
            for (int i = 0; i < oldData.Length; i++)
                I[++buckets[oldData[i]]] = i;

            int[] v = new int[oldData.Length + 1];
            for (int i = 0; i < oldData.Length; i++)
                v[i] = buckets[oldData[i]];

            for (int i = 1; i < 256; i++)
            {
                if (buckets[i] == buckets[i - 1] + 1)
                    I[buckets[i]] = -1;
            }

            I[0] = -1;

            for (int h = 1; I[0] != -(oldData.Length + 1); h += h)
            {
                int len = 0;
                int i = 0;
                while (i < oldData.Length + 1)
                {
                    if (I[i] < 0)
                    {
                        len -= I[i];
                        i -= I[i];
                    }
                    else
                    {
                        if (len != 0)
                            I[i - len] = -len;
                        len = v[I[i]] + 1 - i;
                        Split(I, v, i, len, h);
                        i += len;
                        len = 0;
                    }
                }

                if (len != 0)
                    I[i - len] = -len;
            }

            for (int i = 0; i < oldData.Length + 1; i++)
                I[v[i]] = i;

            return I;
        }

        private static void Swap(ref int first, ref int second)
        {
            int temp = first;
            first = second;
            second = temp;
        }

        private static long ReadInt64(byte[] buf, int offset)
        {
            long value = buf[offset + 7] & 0x7F;

            for (int index = 6; index >= 0; index--)
            {
                value *= 256;
                value += buf[offset + index];
            }

            if ((buf[offset + 7] & 0x80) != 0)
                value = -value;

            return value;
        }

        private static void WriteInt64(long value, byte[] buf, int offset)
        {
            long valueToWrite = value < 0 ? -value : value;

            for (int byteIndex = 0; byteIndex < 8; byteIndex++)
            {
                buf[offset + byteIndex] = (byte) (valueToWrite % 256);
                valueToWrite -= buf[offset + byteIndex];
                valueToWrite /= 256;
            }

            if (value < 0)
                buf[offset + 7] |= 0x80;
        }

        private const long CFileSignature = 0x3034464649445342L;
        private const int CHeaderSize = 32;
    }

    /// <summary>
    /// A <see cref="Stream"/> that wraps another stream. One major feature of <see cref="WrappingStream"/> is that it does not dispose the
    /// underlying stream when it is disposed if Ownership.None is used; this is useful when using classes such as <see cref="BinaryReader"/> and
    /// <see cref="System.Security.Cryptography.CryptoStream"/> that take ownership of the stream passed to their constructors.
    /// </summary>
    /// <remarks>See <a href="http://code.logos.com/blog/2009/05/wrappingstream_implementation.html">WrappingStream Implementation</a>.</remarks>
    public class WrappingStream : Stream
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WrappingStream"/> class.
        /// </summary>
        /// <param name="streamBase">The wrapped stream.</param>
        /// <param name="ownership">Use Owns if the wrapped stream should be disposed when this stream is disposed.</param>
        public WrappingStream(Stream streamBase, Ownership ownership)
        {
            // check parameters
            _mStreamBase = streamBase ?? throw new ArgumentNullException(nameof(streamBase));
            _mOwnership = ownership;
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <returns><c>true</c> if the stream supports reading; otherwise, <c>false</c>.</returns>
        public override bool CanRead => _mStreamBase.CanRead;

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <returns><c>true</c> if the stream supports seeking; otherwise, <c>false</c>.</returns>
        public override bool CanSeek => _mStreamBase.CanSeek;

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <returns><c>true</c> if the stream supports writing; otherwise, <c>false</c>.</returns>
        public override bool CanWrite => _mStreamBase.CanWrite;

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        public override long Length
        {
            get
            {
                ThrowIfDisposed();
                return _mStreamBase.Length;
            }
        }

        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        public override long Position
        {
            get
            {
                ThrowIfDisposed();
                return _mStreamBase.Position;
            }
            set
            {
                ThrowIfDisposed();
                _mStreamBase.Position = value;
            }
        }

        /// <summary>
        /// Begins an asynchronous read operation.
        /// </summary>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback,
            object? state)
        {
            ThrowIfDisposed();
            return _mStreamBase.BeginRead(buffer, offset, count, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous write operation.
        /// </summary>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback,
            object? state)
        {
            ThrowIfDisposed();
            return _mStreamBase.BeginWrite(buffer, offset, count, callback, state);
        }

        /// <summary>
        /// Waits for the pending asynchronous read to complete.
        /// </summary>
        public override int EndRead(IAsyncResult asyncResult)
        {
            ThrowIfDisposed();
            return _mStreamBase.EndRead(asyncResult);
        }

        /// <summary>
        /// Ends an asynchronous write operation.
        /// </summary>
        public override void EndWrite(IAsyncResult asyncResult)
        {
            ThrowIfDisposed();
            _mStreamBase.EndWrite(asyncResult);
        }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            ThrowIfDisposed();
            _mStreamBase.Flush();
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position
        /// within the stream by the number of bytes read.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            return _mStreamBase.Read(buffer, offset, count);
        }

        /// <summary>
        /// Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
        /// </summary>
        public override int ReadByte()
        {
            ThrowIfDisposed();
            return _mStreamBase.ReadByte();
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
        /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();
            return _mStreamBase.Seek(offset, origin);
        }

        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value)
        {
            ThrowIfDisposed();
            _mStreamBase.SetLength(value);
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current position
        /// within this stream by the number of bytes written.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            _mStreamBase.Write(buffer, offset, count);
        }

        /// <summary>
        /// Writes a byte to the current position in the stream and advances the position within the stream by one byte.
        /// </summary>
        public override void WriteByte(byte value)
        {
            ThrowIfDisposed();
            _mStreamBase.WriteByte(value);
        }

        /// <summary>
        /// Gets the wrapped stream.
        /// </summary>
        /// <value>The wrapped stream.</value>
        protected Stream WrappedStream => _mStreamBase;

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="WrappingStream"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            // doesn't close the base stream, but just prevents access to it through this WrappingStream
            if (!disposing)
            {
                return;
            }

            if (_mOwnership == Ownership.Owns)
            {
                _mStreamBase.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Throws if the stream is disposed
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        private void ThrowIfDisposed()
        {
            // throws an ObjectDisposedException if this object has been disposed
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private bool _disposed;
        private readonly Stream _mStreamBase;
        private readonly Ownership _mOwnership;
    }

    /// <summary>
    /// Indicates whether an object takes ownership of an item.
    /// </summary>
    public enum Ownership
    {
        /// <summary>
        /// The object does not own this item.
        /// </summary>
        None,

        /// <summary>
        /// The object owns this item, and is responsible for releasing it.
        /// </summary>
        Owns
    }

    /// <summary>
    /// Provides helper methods for working with <see cref="Stream"/>.
    /// </summary>
    public static class StreamUtility
    {
        /// <summary>
        /// Reads exactly <paramref name="count"/> bytes from <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="count">The count of bytes to read.</param>
        /// <returns>A new byte array containing the data read from the stream.</returns>
        public static byte[] ReadExactly(this Stream stream, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var buffer = new byte[count];
            ReadExactly(stream, buffer, 0, count);
            return buffer;
        }

        /// <summary>
        /// Reads exactly <paramref name="count"/> bytes from <paramref name="stream"/> into
        /// <paramref name="buffer"/>, starting at the byte given by <paramref name="offset"/>.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">The buffer to read data into.</param>
        /// <param name="offset">The offset within the buffer at which data is first written.</param>
        /// <param name="count">The count of bytes to read.</param>
        public static void ReadExactly(this Stream stream, byte[] buffer, int offset, int count)
        {
            // check arguments
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || buffer.Length - offset < count)
                throw new ArgumentOutOfRangeException(nameof(count));

            while (count > 0)
            {
                // read data
                int bytesRead = stream.Read(buffer, offset, count);

                // check for failure to read
                if (bytesRead == 0)
                    throw new EndOfStreamException();

                // move to next block
                offset += bytesRead;
                count -= bytesRead;
            }
        }
    }
}