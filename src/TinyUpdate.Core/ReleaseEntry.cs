using System;
using System.IO;
using TinyUpdate.Core.Exceptions;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Core
{
    /// <summary>
    /// Details about an update that can be applied to the application
    /// </summary>
    public class ReleaseEntry
    {
        private readonly ILogging _logger;

        public ReleaseEntry(string sha1, string filename, long filesize, bool isDelta, Version version, string? filePath = null)
        {
            if (!SHA1Util.IsValidSHA1(sha1))
            {
                throw new Exception("SHA1 hash given is not a valid SHA1 hash");
            }
            if (!filename.IsValidForFileName(out var invalidChar))
            {
                throw new InvalidFilePathException(invalidChar);
            }
            _logger = LoggingCreator.CreateLogger($"ReleaseEntry ({filename})");

            SHA1 = sha1;
            Filename = filename;
            Filesize = filesize;
            IsDelta = isDelta;
            Version = version;
            FileLocation = Path.Combine(filePath ?? Global.TempFolder, Filename);
        }

        //TODO: Replace this with SHA256, just used this so had something workingTM
        /// <summary>
        /// The SHA1 of the file that contains this release
        /// </summary>
        public string SHA1 { get; }

        /// <summary>
        /// The filename of this release
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// How big the file should be
        /// </summary>
        public long Filesize { get; }

        /// <summary>
        /// If this release is a delta update
        /// </summary>
        public bool IsDelta { get; }

        /// <summary>
        /// What <see cref="Version"/> this release will bump the application too
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// The location of the update file
        /// </summary>
        public string FileLocation { get; }

        /// <summary>
        /// Reports if this <see cref="ReleaseEntry"/> is valid and can be applied
        /// </summary>
        /// <param name="checkFile">If we should also check the update file and not just the metadata we have about it and if it's currently on disk</param>
        public virtual bool IsValidReleaseEntry(bool checkFile = false)
        {
            //Check that file exists
            if (!File.Exists(FileLocation))
            {
                _logger.Warning("{0} doesn't exist, this release entry isn't valid", FileLocation);
                return false;
            }
            
            //If we want to check the file then we want to check the SHA1 + file size
            if (checkFile)
            {
                try
                {
                    using var file = File.Open(FileLocation, FileMode.Open);
                    if (file.Length != Filesize ||
                        !SHA1Util.CheckSHA1(file, SHA1))
                    {
                        _logger.Warning("{0} validation failed, this release entry isn't valid", FileLocation);
                        return false;
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                    return false;
                }
            }
            
            //Check that this version is higher then what we are running now
            return Global.ApplicationVersion < Version;
        }
    }
}