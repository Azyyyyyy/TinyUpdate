using System.Runtime.InteropServices;

namespace TinyUpdate.MSDelta.Struct;

[StructLayout(LayoutKind.Sequential)]
internal struct DeltaHash
{
    public uint HashSize;

    //TODO: See why it's not liking this in source gen, ok to skip for now
    //[MarshalAs(UnmanagedType.LPWStr)]
    //public string HashValue;
}