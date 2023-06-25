using System;
using System.Runtime.InteropServices;

namespace TinyUpdate.Core.Native.Windows;

internal static class Invokes
{
    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool CreateHardLink(
        string lpFileName,
        string lpExistingFileName,
        IntPtr lpSecurityAttributes
    );

    /// <summary>
    /// Creates a hard link for a file
    /// </summary>
    /// <param name="originalFile">Where the file that is going to be linked is</param>
    /// <param name="linkLocation">where we want the link to be</param>
    /// <returns>If we was able to make the hard link</returns>
    internal static bool CreateHardLink(string originalFile, string linkLocation) =>
        CreateHardLink(linkLocation, originalFile, IntPtr.Zero);
}