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

            var folder = runningAssembly.Location;
            folder = folder[..folder.LastIndexOf(Path.DirectorySeparatorChar)];
            folder = folder[..folder.LastIndexOf(Path.DirectorySeparatorChar)];
            ApplicationFolder = folder;
            _tempFolder = Path.Combine(_tempFolder, Path.GetFileName(ApplicationFolder));
        }

        /// <summary>
        /// The <see cref="Version"/> that the application is currently running at
        /// </summary>
        public Version ApplicationVersion { get; set; } = new Version();

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

        private string _applicationName;
        public string ApplicationName
        {
            get => _applicationName;
            set
            {
                if (!value.IsValidForFileName(out var invalidChar))
                {
                    throw new InvalidFileNameException(invalidChar);
                }

                _applicationName = value;
            }
        }

        private string _applicationFolder;
        /// <summary>
        /// The folder that contains the application files
        /// </summary>
        public string ApplicationFolder
        {
            get => _applicationFolder;
            set
            {
                if (!Directory.Exists(value))
                {
                    throw new Exception("Folder doesn't exist");
                }

                _applicationFolder = value;
            }
        }
    }
}