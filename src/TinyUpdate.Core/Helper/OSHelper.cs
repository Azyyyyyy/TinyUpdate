using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming

namespace TinyUpdate.Core.Helper
{
    /// <summary>
    /// Small helper to store what OS we are currently running on
    /// </summary>
    public static class OSHelper
    {
        static OSHelper()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ActiveOS = OSPlatform.Windows;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ActiveOS = OSPlatform.Linux;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ActiveOS = OSPlatform.OSX;
            }
        }
        
        /// <summary>
        /// Gets the OS that we are currently running
        /// </summary>
        public static OSPlatform ActiveOS { get; }
    }
}