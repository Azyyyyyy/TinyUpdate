using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using TinyUpdate.Desktop.Abstract;

namespace TinyUpdate.Desktop.Native;

[SupportedOSPlatform("Linux")]
[SupportedOSPlatform("macOS")]
public partial class NativeLinux : INative
{
    [LibraryImport("libc", StringMarshalling = StringMarshalling.Custom, 
        StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
    private static partial int link(
        string oldpath,
        string newpath
    );

    public bool CreateHardLink(string sourcePath, string targetPath) =>
        link(sourcePath, targetPath) == 0;
}