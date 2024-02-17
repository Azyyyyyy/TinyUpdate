using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Text;
using TinyUpdate.Desktop.Abstract;

namespace TinyUpdate.Desktop.Native;

[SupportedOSPlatform("Windows")]
public partial class NativeWindows : INative
{
    [LibraryImport("Kernel32.dll", StringMarshalling = StringMarshalling.Custom, 
        StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CreateHardLinkA(
        string lpFileName,
        string lpExistingFileName,
        IntPtr lpSecurityAttributes
    );

    public bool CreateHardLink(string sourcePath, string targetPath) =>
        CreateHardLinkA(targetPath, sourcePath, IntPtr.Zero);
}