using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TinyUpdate.Desktop.Abstract;

namespace TinyUpdate.Desktop.Native;

[SupportedOSPlatform("Windows")]
public partial class NativeWindows : INative
{
    [return: MarshalAs(UnmanagedType.Bool)]
    [LibraryImport("Kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial bool CreateHardLink(
        string lpFileName,
        string lpExistingFileName,
        IntPtr lpSecurityAttributes
    );

    public bool CreateHardLink(string sourcePath, string targetPath) =>
        CreateHardLink(targetPath, sourcePath, IntPtr.Zero);
}