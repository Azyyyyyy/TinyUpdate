using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using TinyUpdate.Core.Exceptions;
using TinyUpdate.Core.Utils;

[assembly: InternalsVisibleTo("TinyUpdate.Test")]
namespace TinyUpdate.Core
{
    //TODO: Remove internal sets when not needed, for now just there to make testing less of a pain
    /// <summary>
    /// Anything that needs to be accessed from anywhere in the library
    /// </summary>
    public static class Global
    {
        static Global()
        {
            //Get the assembly, check that a version number exists and that we can make a Version out of it
            var runningAssembly = Assembly.GetEntryAssembly();

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
             is the same as the version number (as this is an hint that we aren't installed 
             as we should be), note that we don't want to do this check in a Unit Test*/
            var uri = new UriBuilder(runningAssembly.CodeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            if (!DebugUtil.IsInUnitTest && Path.GetFileName(Path.GetDirectoryName(path)) != $"app-{versionString}")
            {
                throw new Exception("We haven't been installed correctly");
            }

            ApplicationFolder = Path.GetDirectoryName(Path.GetDirectoryName(path));
            _tempFolder = Path.Combine(_tempFolder, Path.GetFileName(ApplicationFolder));
        }

        /// <summary>
        /// Extension to be used on any update file that we create
        /// </summary>
        public const string TinyUpdateExtension = ".tuup";
        
        /// <summary>
        /// The <see cref="Version"/> that the application is currently running at
        /// </summary>
        public static Version ApplicationVersion { get; internal set; }

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
                _tempFolder = Path.Combine(value, Path.GetFileName(ApplicationFolder));
            }
        }

        /// <summary>
        /// The folder that contains the application files
        /// </summary>
        public static string ApplicationFolder { get; internal set; }
    }
}