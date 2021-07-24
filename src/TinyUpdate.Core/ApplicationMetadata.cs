using System;
using System.IO;
using System.Reflection;
using TinyUpdate.Core.Exceptions;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Core
{
    /// <summary>
    /// Anything that needs to be accessed from anywhere in the library
    /// </summary>
    public class ApplicationMetadata
    {
        private readonly ILogging _logging = LoggingCreator.CreateLogger(nameof(ApplicationMetadata));
        
        public ApplicationMetadata()
        {
            //Get the assembly if possible and get data out of it
            var runningAssembly = Assembly.GetEntryAssembly();
            if (runningAssembly == null)
            {
                _logging.Warning("Can't get running assembly, will not have any metadata to work with!");
                return;
            }
            var applicationName = runningAssembly.GetName();
            ApplicationVersion = applicationName.Version;
            ApplicationName = applicationName.Name;
            
            var uri = new UriBuilder(runningAssembly.CodeBase);
            var path = Uri.UnescapeDataString(uri.Path);

            //TODO: Do this more in a more efficient way
            ApplicationFolder = Path.GetDirectoryName(Path.GetDirectoryName(path)) ?? "";
            _tempFolder = Path.Combine(_tempFolder, Path.GetFileName(ApplicationFolder));
        }

        /// <summary>
        /// The <see cref="Version"/> that the application is currently running at
        /// </summary>
        public Version ApplicationVersion { get; set; }

        private string _tempFolder = Path.Combine(Path.GetTempPath(), "TinyUpdate");
        /// <summary>
        /// The folder to be used when downloading/creating any files that are only needed for a short period of time
        /// </summary>
        public string TempFolder
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
        public string ApplicationName { get; set; }

        private string _applicationFolder;
        /// <summary>
        /// The folder that contains the application files
        /// </summary>
        public string ApplicationFolder
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