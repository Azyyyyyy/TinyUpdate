using System.Runtime.InteropServices;
using TinyUpdate.Core;

namespace TinyUpdate.Delta.MSDelta.Struct;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DeltaHash
{
    public int HashSize; // do not exceed 32 (DELTA_MAX_HASH_SIZE)
    public fixed byte HashValue[32];

    public string GetHash()
    {
        fixed (byte* hashPtr = HashValue)
        {
            using var unmanagedStream = new UnmanagedMemoryStream(hashPtr, HashSize);
            return SHA256.Instance.CreateHash(unmanagedStream);
        }
    }
}