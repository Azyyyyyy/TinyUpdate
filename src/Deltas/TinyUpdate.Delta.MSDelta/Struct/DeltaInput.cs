using System.Runtime.InteropServices;

namespace TinyUpdate.Delta.MSDelta.Struct;

/// <summary>
///     Type for input memory blocks
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DeltaInput(byte* buffer, IntPtr length, bool editable)
{
    /// <summary>
    /// Start of memory block
    /// </summary>
    public byte* Buffer = buffer;
    
    /// <summary>
    /// Size of memory block in bytes
    /// </summary>
    public IntPtr Length = length; // SIZE_T, so different size on x86/x64
    
    /// <summary>
    /// If the caller allows msdelta to edit this memory block
    /// </summary>
    [MarshalAs(UnmanagedType.Bool)] 
    public bool Editable = editable;

    /// <summary>
    /// An empty <see cref="DeltaInput"/>
    /// </summary>
    public static DeltaInput Empty = new();
}