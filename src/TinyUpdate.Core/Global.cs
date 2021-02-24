using System;
using System.IO;
using System.Reflection;

namespace TinyUpdate.Core
{
    /// <summary>
    /// Anything that needs to be accessed from anywhere in the library
    /// </summary>
    public static class Global
    {
        static Global()
        {
            //Get the assembly, check that a version number exists and that we can make a Version out of it
            var runningAssembly = Assembly.GetExecutingAssembly();

            var versionString = runningAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            if (string.IsNullOrWhiteSpace(versionString))
            {
                throw new NotImplementedException("The executing assembly doesn't have a version attached to it");
            }
            if (!Version.TryParse(versionString, out var version))
            {
                throw new Exception("We can't make a Version out of the version string that is in the assembly");
            }
            ApplicationVersion = version;

            /*Now grab where the application is installed, checking that the current folder
             is the same as the version number (as this is an hint that we aren't installed as we should be)*/
            var uri = new UriBuilder(runningAssembly.CodeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            if (Path.GetFileName(path) != versionString)
            {
                throw new Exception("We haven't been installed correctly");
            }
            
            ApplicationFolder = Path.GetDirectoryName(Path.GetDirectoryName(path));
        }

        /// <summary>
        /// The version that the application is currently running at
        /// </summary>
        public static Version ApplicationVersion { get; }

        /// <summary>
        /// The folder to be used when downloading/creating any file that is only needed for a short period of time
        /// </summary>
        public static string TempFolder { get; set; } = Path.Combine(Path.GetTempPath(), "TinyUpdate");

        /// <summary>
        /// The folder that contains the application
        /// </summary>
        public static string ApplicationFolder { get; }
    }
}