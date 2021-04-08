namespace TinyUpdate.Core.Helper
{
    /// <summary>
    /// Helper for creating Hard links
    /// </summary>
    public static class HardLinkHelper
    {
        /// <summary>
        /// Creates a hard link for a file
        /// </summary>
        /// <param name="originalFile">Where the file is located that you want to make a hard link for</param>
        /// <param name="linkLocation">Where you want to have the hard link to be</param>
        /// <returns>If it was able to create a hard link</returns>
#if Windows
        public static bool CreateHardLink(string originalFile, string linkLocation) =>
            Native.Windows.Invoke.CreateHardLink(originalFile, linkLocation);
#elif Linux
        public static bool CreateHardLink(string originalFile, string linkLocation) =>
            Native.Linux.Invokes.CreateHardLink(originalFile, linkLocation);
#elif macOS
        public static bool CreateHardLink(string originalFile, string linkLocation) => false;
#else
        public static bool CreateHardLink(string originalFile, string linkLocation)
        {
            //Invokes the correct logic based on OS
            return TaskHelper.RunTaskBasedOnOS(
                () => Native.Windows.Invoke.CreateHardLink(originalFile, linkLocation),
                () => Native.Linux.Invokes.CreateHardLink(originalFile, linkLocation),
                () => false); //Apple doesn't like people doing hard links, no point in even trying :KEK:
        }
#endif
    }
}