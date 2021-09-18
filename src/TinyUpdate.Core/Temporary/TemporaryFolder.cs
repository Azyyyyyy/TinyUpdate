using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Core.Temporary
{
    public class TemporaryFolder : IDisposable
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(TemporaryFolder));
        private readonly List<TemporaryFile> _files = new List<TemporaryFile>();
        private readonly List<TemporaryFolder> _folders = new List<TemporaryFolder>();

        /// <summary>
        /// A temporary folder which will contain any files/folders for a short amount of time
        /// </summary>
        /// <param name="folderLocation">Where the folder is</param>
        /// <param name="deleteFolder">If we want to delete the folder before and after</param>
        public TemporaryFolder(string? folderLocation = null, bool deleteFolder = true)
        {
            Location = folderLocation ?? Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            _deleteFolder = deleteFolder;
            if (deleteFolder && Directory.Exists(folderLocation))
            {
                Directory.Delete(Location, true);
            }
            Directory.CreateDirectory(Location);
        }
        
        public string Location { get; }

        public override string ToString() => Location;

        public TemporaryFile CreateTemporaryFile(string? filename = null)
        {
            var fileLocation = Path.Combine(Location, filename ?? Path.GetRandomFileName());
            var tempFile = new TemporaryFile(fileLocation);
            _files.Add(tempFile);
            return tempFile;
        }
        
        public TemporaryFolder CreateTemporaryFolder(string? folderName = null)
        {
            var fileLocation = Path.Combine(Location, folderName ?? Path.GetRandomFileName());
            var tempFile = new TemporaryFolder(fileLocation, _deleteFolder);
            _folders.Add(tempFile);
            return tempFile;
        }

        private bool _disposed;
        private readonly bool _deleteFolder;
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            
            foreach (var file in _files)
            {
                file.Dispose();
            }
            foreach (var folder in _folders)
            {
                folder.Dispose();
            }

            /*Don't clear the folder if we won't want to
             or if the folder doesn't exist by the time we get here*/
            if (!_deleteFolder || !Directory.Exists(Location))
            {
                return;
            }
            
            if (Directory.EnumerateFiles(Location)
                .Concat(Directory.EnumerateDirectories(Location)).Any())
            {
                Logger.Warning("{0} still contains some files, these will be deleted!", Location);
            }

            try
            {
                Directory.Delete(Location, true);
            }
            catch (Exception e)
            {
                Logger.Error("Something happened while trying to cleanup {0}, this folder might still exist and need manual cleanup!", Location);
                Logger.Error(e);
            }
        }
    }
}