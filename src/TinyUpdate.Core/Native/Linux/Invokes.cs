using System.Runtime.InteropServices;

namespace TinyUpdate.Core.Native.Linux
{
    public class Invokes
    {
        [DllImport("libc", SetLastError = true)]
        private static extern int link(
            string oldpath,
            string newpath
        );

        /// <summary>
        /// Creates a hard link for a file
        /// </summary>
        /// <param name="originalFile">Where the file that is going to be linked is</param>
        /// <param name="linkLocation">where we want the link to be</param>
        /// <returns>If we was able to make the hard link</returns>
        internal static bool CreateHardLink(string originalFile, string linkLocation) => link(originalFile, linkLocation) == 0;
    }
}