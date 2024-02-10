using System.Runtime.InteropServices;
using TinyUpdate.Core;
using TinyUpdate.Core.Services;

namespace TinyUpdate.Delta.MSDelta.Struct;

/// <summary>
/// Hash structure
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DeltaHash
{
    private const int DeltaMaxHashSize = 32;
    
    /// <summary>
    /// Size of hash in bytes (does not exceed <see cref="DeltaMaxHashSize"/>)
    /// </summary>
    public int HashSize;
    
    /// <summary>
    /// Hash value
    /// </summary>
    public fixed byte HashValue[DeltaMaxHashSize];

    /// <summary>
    /// Gets the <see cref="DeltaHash"/> as a <see cref="SHA256"/>
    /// </summary>
    public string GetHash()
    {
        fixed (byte* hashPtr = HashValue)
        {
            using var unmanagedStream = new UnmanagedMemoryStream(hashPtr, HashSize);
            return SHA256.Instance.HashData(unmanagedStream);
        }
    }
}