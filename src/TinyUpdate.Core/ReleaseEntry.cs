using System.IO;
using SemVersion;
using TinyUpdate.Core.Exceptions;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Update;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Core
{
    /// <summary>
    /// Details about an update that can be applied to the application
    /// </summary>
    public class ReleaseEntry
    {
        private readonly ILogging _logger;
        public ReleaseEntry(
            string sha256,
            string filename,
            long filesize,
            bool isDelta,
            SemanticVersion version,
            string folderPath,
            SemanticVersion? oldVersion = null,
            string? tag = null,
            int? stagingPercentage = null)
        {
            //If it's a delta file then we should also be given an oldVersion
            if (isDelta)
            {
                if (oldVersion == null)
                {
                    throw new OldVersionRequiredException();
                }

                OldVersion = oldVersion;
            }

            if (filesize < 0)
            {
                throw new BadFilesizeException();
            }

            //Check hash and file name/path
            if (!SHA256Util.IsValidSHA256(sha256))
            {
                throw new InvalidHashException();
            }

            if (!filename.IsValidForFileName(out var invalidChar))
            {
                throw new InvalidFileNameException(invalidChar);
            }
            if (!folderPath.IsValidForFilePath(out var invalidPathChar))
            {
                throw new InvalidFilePathException(invalidPathChar);
            }

            _logger = LoggingCreator.CreateLogger($"{nameof(ReleaseEntry)} ({filename})");

            SHA256 = sha256;
            Filename = filename;
            Filesize = filesize;
            IsDelta = isDelta;
            Version = version;
            FileLocation = Path.Combine(folderPath, Filename);
            StagingPercentage = stagingPercentage;
            Tag = tag;
        }

        public int? StagingPercentage { get; }

        /// <summary>
        /// Tag that a <see cref="UpdateClient"/> can use to store some extra data that is needed
        /// </summary>
        public string? Tag { get; }

        /// <summary>
        /// The SHA256 of the file that contains this release
        /// </summary>
        public string SHA256 { get; }

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
        /// What <see cref="SemanticVersion"/> this release will bump the application too
        /// </summary>
        public SemanticVersion Version { get; }

        /// <summary>
        /// The version used to create this <see cref="ReleaseEntry"/> (If this is a delta update)
        /// </summary>
        public SemanticVersion? OldVersion { get; }

        /// <summary>
        /// The location of the update file
        /// </summary>
        public string FileLocation { get; }

        /// <summary>
        /// Reports if this <see cref="ReleaseEntry"/> is valid and can be applied
        /// </summary>
        /// <param name="applicationVersion">What is the application version is</param>
        /// <param name="checkFile">If we should also check the update file and not just the metadata we have about it and if it's currently on disk</param>
        public virtual bool IsValidReleaseEntry(SemanticVersion applicationVersion, bool checkFile = false)
        {
            //If we want to check the file then we want to check the SHA256 + file size
            if (checkFile)
            {
                //Check that file exists
                if (!File.Exists(FileLocation))
                {
                    _logger.Warning("{0} doesn't exist, this release entry isn't valid", FileLocation);
                    return false;
                }

                using var file = StreamUtil.SafeOpenRead(FileLocation);
                if (file?.Length != Filesize ||
                    !SHA256Util.CheckSHA256(file, SHA256))
                {
                    file?.Dispose();
                    _logger.Warning("{0} validation failed, this release entry isn't valid", FileLocation);
                    return false;
                }
                file.Dispose();
            }

            //Check that this Version is higher then what we are running now
            return applicationVersion < Version;
        }
    }
}