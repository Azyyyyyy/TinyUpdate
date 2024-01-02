using System.Runtime.InteropServices;

namespace TinyUpdate.MSDelta.Struct;

/// <summary>
///     Type for input memory blocks
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DeltaInput(byte* pBuf_, int cbBuf_, bool editable_)
{
    public byte* pBuf = pBuf_;
    public IntPtr cbBuf = new IntPtr(cbBuf_); // SIZE_T, so different size on x86/x64
    [MarshalAs(UnmanagedType.Bool)] public bool editable = editable_;

    public static DeltaInput Empty = new();
}