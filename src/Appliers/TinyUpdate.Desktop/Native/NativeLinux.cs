using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TinyUpdate.Desktop.Abstract;

namespace TinyUpdate.Desktop.Native;

[SupportedOSPlatform("Linux")]
public partial class NativeLinux : INative
{
    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial int link(
        string oldpath,
        string newpath
    );

    public bool CreateHardLink(string sourcePath, string targetPath) =>
        link(sourcePath, targetPath) == 0;
}