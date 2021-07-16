using System;
using System.IO;
using System.Reflection;
using TinyUpdate.Core.Exceptions;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Core
{
    //TODO: Make this not static
    /// <summary>
    /// Anything that needs to be accessed from anywhere in the library
    /// </summary>
    public static class Global
    {
        static Global()
        {
            //Get the assembly, check that a version number exists and that we can make a Version out of it
            var runningAssembly = Assembly.GetEntryAssembly();
            if (runningAssembly == null)
            {
                throw new Exception("We somehow can't get the currently running assembly");
            }

            var applicationName = runningAssembly.GetName();
            ApplicationVersion = applicationName.Version;
            ApplicationName = applicationName.Name;


            /*Now grab where the application is installed, checking that the current folder
             is the same as the version number (as this is an hint that we aren't installed 
             as we should be), note that we don't want to do this check in a Unit Test*/
            var uri = new UriBuilder(runningAssembly.CodeBase);
            var path = Uri.UnescapeDataString(uri.Path);
#if !DEBUG
            if (!DebugUtil.IsInUnitTest
                && Path.GetFileName(Path.GetDirectoryName(path)) != $"app-{ApplicationVersion.ToString(4)}")
            {
                throw new Exception("We haven't been installed correctly");
            }
#endif
            ApplicationFolder = Path.GetDirectoryName(Path.GetDirectoryName(path));
            _tempFolder = Path.Combine(_tempFolder, Path.GetFileName(ApplicationFolder));
        }

        /// <summary>
        /// The <see cref="Version"/> that the application is currently running at
        /// </summary>
        public static Version ApplicationVersion { get; set; }

        private static string _tempFolder = Path.Combine(Path.GetTempPath(), "TinyUpdate");

        /// <summary>
        /// The folder to be used when downloading/creating any files that are only needed for a short period of time
        /// </summary>
        public static string TempFolder
        {
            get => _tempFolder;
            set
            {
                if (!value.IsValidForFilePath(out var invalidChar))
                {
                    throw new InvalidFilePathException(invalidChar);
                }

                _tempFolder = Path.Combine(value, ApplicationName);
            }
        }

        //TODO: Check for name being File path/name safe
        public static string ApplicationName { get; set; }

        private static string _applicationFolder;

        /// <summary>
        /// The folder that contains the application files
        /// </summary>
        public static string ApplicationFolder
        {
            get => _applicationFolder;
            set
            {
                if (Directory.Exists(value))
                {
                    _applicationFolder = value;
                    return;
                }

                throw new Exception("Folder doesn't exist");
            }
        }
    }
}