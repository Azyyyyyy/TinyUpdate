using Microsoft.Extensions.Logging;
using SharpCompress.Compressors;
using SharpCompress.Compressors.BZip2;
using TinyUpdate.Core.Abstract.Delta;

//TODO: Bring in changes from bsdiff.net into here
// Squirrel.Bsdiff: Adapted from https://github.com/LogosBible/bsdiff.net/blob/master/src/bsdiff/BinaryPatchUtility.cs
// TinyUpdate: Adapted from https://github.com/Squirrel/Squirrel.Windows/blob/develop/src/Squirrel/BinaryPatchUtility.cs
namespace TinyUpdate.Delta.BSDiff;

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

/// <summary>
/// Provides creating and applying BSDiff delta's
/// </summary>
public partial class BSDiffDelta(ILogger logger) : IDeltaApplier, IDeltaCreation
{
    private const long CFileSignature = 0x3034464649445342L;
    private const int CHeaderSize = 32;

    public string Extension => ".bsdiff";

    public bool SupportedStream(Stream deltaStream) => SupportedStream(deltaStream, out _);

    public long TargetStreamSize(Stream deltaStream)
    {
        SupportedStream(deltaStream, out var newSize);
        return newSize;
    }
    
    //TODO: Add Source + Target size check
    public async Task<bool> ApplyDeltaFile(Stream sourceStream, Stream deltaStream, Stream targetStream, IProgress<double>? progress = null)
    {
        // check patch stream capabilities
        if (!deltaStream.CanRead)
        {
            logger.LogError("Patch stream must be readable");
            return false;
        }
        if (!deltaStream.CanSeek)
        {
            logger.LogError("Patch stream must be seekable");
            return false;
        }
        if (deltaStream.Length == 0)
        {
            logger.LogError("Patch stream must contain something");
            return false;
        }

        //TODO: Check stream type passed
        await using var patchMemoryStream = new MemoryStream();
        await deltaStream.CopyToAsync(patchMemoryStream);

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
        await using (var patchStream = CreatePatchStream())
        {
            var header = new byte[CHeaderSize];
            patchStream.ReadExactly(header);

            // check for appropriate magic
            var signature = ReadInt64(header, 0);
            if (signature != CFileSignature)
            {
                logger.LogError("Corrupt patch");
                return false;
            }

            // read lengths from header
            controlLength = ReadInt64(header, 8);
            diffLength = ReadInt64(header, 16);
            newSize = ReadInt64(header, 24);
            if (controlLength < 0 || diffLength < 0 || newSize < 0)
            {
                logger.LogError("Corrupt patch");
                return false;
            }
        }

        // preallocate buffers for reading and writing
        const int cBufferSize = 1048576;
        var newData = new byte[cBufferSize];
        var oldData = new byte[cBufferSize];

        // prepare to read three parts of the patch in parallel
        await using var compressedControlStream = CreatePatchStream();
        await using var compressedDiffStream = CreatePatchStream();
        await using var compressedExtraStream = CreatePatchStream();
        {
            // seek to the start of each part
            compressedControlStream.Seek(CHeaderSize, SeekOrigin.Current);
            compressedDiffStream.Seek(CHeaderSize + controlLength, SeekOrigin.Current);
            compressedExtraStream.Seek(CHeaderSize + controlLength + diffLength, SeekOrigin.Current);

            // decompress each part (to read it)
            await using var controlStream = new BZip2Stream(compressedControlStream, CompressionMode.Decompress, false);
            await using var diffStream = new BZip2Stream(compressedDiffStream, CompressionMode.Decompress, false);
            await using var extraStream = compressedExtraStream.Position != compressedExtraStream.Length
                ? new BZip2Stream(compressedExtraStream, CompressionMode.Decompress, false)
                : Stream.Null;
            var control = new long[3];
            var buffer = new byte[8];

            var oldPosition = 0;
            var newPosition = 0d;
            while (newPosition < newSize)
            {
                if (newPosition != 0) progress?.Report(newSize / newPosition);

                // read control data
                for (var i = 0; i < 3; i++)
                {
                    await controlStream.ReadExactlyAsync(buffer.AsMemory(0, 8));
                    control[i] = ReadInt64(buffer, 0);
                }

                // sanity-check
                if (newPosition + control[0] > newSize)
                {
                    logger.LogError("Corrupt patch");
                    return false;
                }

                // seek old file to the position that the new data is diffed against
                sourceStream.Position = oldPosition;

                var bytesToCopy = (int)control[0];
                while (bytesToCopy > 0)
                {
                    var actualBytesToCopy = Math.Min(bytesToCopy, cBufferSize);

                    // read diff string
                    await diffStream.ReadExactlyAsync(newData.AsMemory(0, actualBytesToCopy));

                    // add old data to diff string
                    var availableOriginalStreamBytes = Math.Min(actualBytesToCopy, (int)(sourceStream.Length - sourceStream.Position));
                    await sourceStream.ReadExactlyAsync(oldData.AsMemory(0, availableOriginalStreamBytes));

                    for (var index = 0; index < availableOriginalStreamBytes; index++)
                        newData[index] += oldData[index];

                    await targetStream.WriteAsync(newData.AsMemory(0, actualBytesToCopy));

                    // adjust counters
                    newPosition += actualBytesToCopy;
                    oldPosition += actualBytesToCopy;
                    bytesToCopy -= actualBytesToCopy;
                }

                // sanity-check
                if (newPosition + control[1] > newSize)
                {
                    logger.LogError("Corrupt patch");
                    return false;
                }

                // read extra string
                bytesToCopy = (int)control[1];
                while (bytesToCopy > 0)
                {
                    var actualBytesToCopy = Math.Min(bytesToCopy, cBufferSize);

                    await extraStream.ReadExactlyAsync(newData.AsMemory(0, actualBytesToCopy));
                    await targetStream.WriteAsync(newData.AsMemory(0, actualBytesToCopy));

                    newPosition += actualBytesToCopy;
                    bytesToCopy -= actualBytesToCopy;
                }

                // adjust position
                oldPosition = (int)(oldPosition + control[2]);
            }
        }

        return true;

        Stream CreatePatchStream()
        {
            //Copy the files over in a memory stream
            var memStream = new MemoryStream();
            patchMemoryStream.Seek(0, SeekOrigin.Begin);
            patchMemoryStream.CopyTo(memStream);
            memStream.Seek(0, SeekOrigin.Begin);

            return memStream;
        }
    }

    //TODO: Add Source + Target size check
    public async Task<bool> CreateDeltaFile(Stream sourceStream, Stream targetStream, Stream deltaStream, IProgress<double>? progress = null)
    {
        // check arguments
        if (!deltaStream.CanSeek)
        {
            logger.LogError("Patch stream must be seekable");
            return false;
        }

        if (!deltaStream.CanWrite)
        {
            logger.LogError("Patch stream must be writable");
            return false;
        }

        // get data
        var oldData = new byte[sourceStream.Length];
        await sourceStream.ReadExactlyAsync(oldData.AsMemory(0, oldData.Length));

        var newData = new byte[targetStream.Length];
        await targetStream.ReadExactlyAsync(newData.AsMemory(0, newData.Length));
        
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
        var header = new byte[CHeaderSize];
        WriteInt64(CFileSignature, header, 0); // "BSDIFF40"
        WriteInt64(0, header, 8);
        WriteInt64(0, header, 16);
        WriteInt64(newData.Length, header, 24);

        var startPosition = deltaStream.Position;
        await deltaStream.WriteAsync(header);

        var I = SuffixSort(oldData);
        progress?.Report(0.5d);

        var db = new byte[newData.Length];
        var eb = new byte[newData.Length];

        var dbLen = 0;
        var ebLen = 0;

        await using (var wrappingStream = new WrappingStream(deltaStream, Ownership.None))
        await using (var bz2Stream = new BZip2Stream(wrappingStream, CompressionMode.Compress, false))
        {
            // compute the differences, writing ctrl as we go
            var scan = 0;
            var pos = 0;
            var len = 0;
            var lastScan = 0;
            var lastPos = 0;
            var lastOffset = 0;
            while (scan < newData.Length)
            {
                progress?.Report((double)scan / newData.Length / 2 + 0.5d);

                var oldScore = 0;

                for (var scsc = scan += len; scan < newData.Length; scan++)
                {
                    len = Search(I, oldData, newData, scan, 0, oldData.Length, out pos);

                    for (; scsc < scan + len; scsc++)
                        if (scsc + lastOffset < oldData.Length && oldData[scsc + lastOffset] == newData[scsc])
                            oldScore++;

                    if ((len == oldScore && len != 0) || len > oldScore + 8)
                        break;

                    if (scan + lastOffset < oldData.Length && oldData[scan + lastOffset] == newData[scan])
                        oldScore--;
                }

                if (len != oldScore || scan == newData.Length)
                {
                    var s = 0;
                    var sf = 0;
                    var lenf = 0;
                    for (var i = 0; lastScan + i < scan && lastPos + i < oldData.Length;)
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

                    var lenb = 0;
                    if (scan < newData.Length)
                    {
                        s = 0;
                        var sb = 0;
                        for (var i = 1; scan >= lastScan + i && pos >= i; i++)
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
                        var overlap = lastScan + lenf - (scan - lenb);
                        s = 0;
                        var ss = 0;
                        var lens = 0;
                        for (var i = 0; i < overlap; i++)
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

                    for (var i = 0; i < lenf; i++)
                        db[dbLen + i] = (byte)(newData[lastScan + i] - oldData[lastPos + i]);
                    for (var i = 0; i < scan - lenb - (lastScan + lenf); i++)
                        eb[ebLen + i] = newData[lastScan + lenf + i];

                    dbLen += lenf;
                    ebLen += scan - lenb - (lastScan + lenf);

                    var buf = new byte[8];
                    WriteInt64(lenf, buf, 0);
                    bz2Stream.Write(buf, 0, 8);

                    WriteInt64(scan - lenb - (lastScan + lenf), buf, 0);
                    bz2Stream.Write(buf, 0, 8);

                    WriteInt64(pos - lenb - (lastPos + lenf), buf, 0);
                    bz2Stream.Write(buf, 0, 8);

                    lastScan = scan - lenb;
                    lastPos = pos - lenb;
                    lastOffset = pos - scan;
                }
            }
        }

        // compute size of compressed ctrl data
        var controlEndPosition = deltaStream.Position;
        WriteInt64(controlEndPosition - startPosition - CHeaderSize, header, 8);

        // write compressed diff data
        if (dbLen > 0)
        {
            await using var wrappingStream = new WrappingStream(deltaStream, Ownership.None);
            await using var bz2Stream = new BZip2Stream(wrappingStream, CompressionMode.Compress, false);
            bz2Stream.Write(db, 0, dbLen);
        }

        // compute size of compressed diff data
        var diffEndPosition = deltaStream.Position;
        WriteInt64(diffEndPosition - controlEndPosition, header, 16);

        // write compressed extra data
        if (ebLen > 0)
        {
            await using var wrappingStream = new WrappingStream(deltaStream, Ownership.None);
            await using var bz2Stream = new BZip2Stream(wrappingStream, CompressionMode.Compress, false);
            bz2Stream.Write(eb, 0, ebLen);
        }

        // seek to the beginning, write the header, then seek back to end
        var endPosition = deltaStream.Position;
        deltaStream.Position = startPosition;
        await deltaStream.WriteAsync(header);
        deltaStream.Position = endPosition;

        return true;
    }
    
    private static bool SupportedStream(Stream deltaStream, out long newSize)
    {
        newSize = -1;
        if (!deltaStream.CanSeek || deltaStream.Length == 0)
        {
            return false;
        }

        var header = new byte[CHeaderSize];
        deltaStream.ReadExactly(header);

        // check for appropriate magic
        var signature = ReadInt64(header, 0);
        if (signature != CFileSignature)
        {
            return false;
        }

        // read lengths from header
        var controlLength = ReadInt64(header, 8);
        var diffLength = ReadInt64(header, 16);
        newSize = ReadInt64(header, 24);
        
        return controlLength >= 0 && diffLength >= 0 && newSize >= 0;
    }
}

public partial class BSDiffDelta
{
    private static int CompareBytes(byte[] left, int leftOffset, byte[] right, int rightOffset)
    {
        for (var index = 0; index < left.Length - leftOffset && index < right.Length - rightOffset; index++)
        {
            var diff = left[index + leftOffset] - right[index + rightOffset];
            if (diff != 0)
                return diff;
        }

        return 0;
    }

    private static int MatchLength(byte[] oldData, int oldOffset, byte[] newData, int newOffset)
    {
        int i;
        for (i = 0; i < oldData.Length - oldOffset && i < newData.Length - newOffset; i++)
            if (oldData[i + oldOffset] != newData[i + newOffset])
                break;

        return i;
    }

    private static int Search(int[] I, byte[] oldData, byte[] newData, int newOffset, int start, int end,
        out int pos)
    {
        while (true)
        {
            if (end - start < 2)
            {
                var startLength = MatchLength(oldData, I[start], newData, newOffset);
                var endLength = MatchLength(oldData, I[end], newData, newOffset);

                if (startLength > endLength)
                {
                    pos = I[start];
                    return startLength;
                }

                pos = I[end];
                return endLength;
            }

            var midPoint = start + (end - start) / 2;
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
                for (var k = start; k < start + len; k += j)
                {
                    j = 1;
                    var x = v[I[k] + h];
                    for (var i = 1; k + i < start + len; i++)
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

                    for (var i = 0; i < j; i++) v[I[k + i]] = k + j - 1;
                    if (j == 1) I[k] = -1;
                }
            }
            else
            {
                var x = v[I[start + len / 2] + h];
                var jj = 0;
                var kk = 0;
                for (var i2 = start; i2 < start + len; i2++)
                {
                    if (v[I[i2] + h] < x) jj++;
                    if (v[I[i2] + h] == x) kk++;
                }

                jj += start;
                kk += jj;

                var i = start;
                var j = 0;
                var k = 0;
                while (i < jj)
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

                while (jj + j < kk)
                    if (v[I[jj + j] + h] == x)
                    {
                        j++;
                    }
                    else
                    {
                        Swap(ref I[jj + j], ref I[kk + k]);
                        k++;
                    }

                if (jj > start) Split(I, v, start, jj - start, h);

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
        var buckets = new int[256];

        foreach (var oldByte in oldData)
            buckets[oldByte]++;
        for (var i = 1; i < 256; i++)
            buckets[i] += buckets[i - 1];
        for (var i = 255; i > 0; i--)
            buckets[i] = buckets[i - 1];
        buckets[0] = 0;

        var I = new int[oldData.Length + 1];
        for (var i = 0; i < oldData.Length; i++)
            I[++buckets[oldData[i]]] = i;

        var v = new int[oldData.Length + 1];
        for (var i = 0; i < oldData.Length; i++)
            v[i] = buckets[oldData[i]];

        for (var i = 1; i < 256; i++)
            if (buckets[i] == buckets[i - 1] + 1)
                I[buckets[i]] = -1;

        I[0] = -1;

        for (var h = 1; I[0] != -(oldData.Length + 1); h += h)
        {
            var len = 0;
            var i = 0;
            while (i < oldData.Length + 1)
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

            if (len != 0)
                I[i - len] = -len;
        }

        for (var i = 0; i < oldData.Length + 1; i++)
            I[v[i]] = i;

        return I;
    }

    private static void Swap(ref int first, ref int second) => (first, second) = (second, first);

    private static long ReadInt64(byte[] buf, int offset)
    {
        long value = buf[offset + 7] & 0x7F;

        for (var index = 6; index >= 0; index--)
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
        var valueToWrite = value < 0 ? -value : value;

        for (var byteIndex = 0; byteIndex < 8; byteIndex++)
        {
            buf[offset + byteIndex] = (byte)(valueToWrite % 256);
            valueToWrite -= buf[offset + byteIndex];
            valueToWrite /= 256;
        }

        if (value < 0)
            buf[offset + 7] |= 0x80;
    }
}