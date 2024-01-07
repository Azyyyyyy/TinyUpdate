using System.Runtime.InteropServices;

namespace TinyUpdate.Delta.MSDelta.Struct;

/// <summary>
///     Type for input memory blocks
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct DeltaOutput
{
    /// <summary>Memory address</summary>
    public IntPtr Start;

    /// <summary>Size of the memory buffer in bytes.</summary>
    public IntPtr Size;
}