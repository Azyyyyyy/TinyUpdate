using System;
using System.IO;
using SemVersion;
using TinyUpdate.Core.Exceptions;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Update;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Core;

/// <summary>
/// Details about an update that can be applied to the application
/// </summary>
public class ReleaseEntry
{
    private readonly ILogger _logger;
    public ReleaseEntry(
        string sha256,
        string filename,
        long filesize,
        bool isDelta,
        SemanticVersion version,
        string folderPath,
        SemanticVersion? oldVersion = null,
        object? tag = null,
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

        _logger = LogManager.CreateLogger($"{nameof(ReleaseEntry)} ({filename})");

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
    public object? Tag { get; }

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
                _logger.Log(Level.Warn, $"{FileLocation} doesn't exist, this release entry isn't valid");
                return false;
            }

            var file = StreamUtil.SafeOpenRead(FileLocation);
            if (file?.Length != Filesize ||
                !SHA256Util.CheckSHA256(file, SHA256))
            {
                file?.Dispose();
                _logger.Log(Level.Warn, $"{FileLocation} validation failed, this release entry isn't valid");
                return false;
            }
            file.Dispose();
        }

        //Check that this Version is higher then what we are running now
        return applicationVersion < Version;
    }

    public override bool Equals(object? obj) => 
        obj is ReleaseEntry releaseEntry && Equals(releaseEntry);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = _logger.GetHashCode();
            hashCode = (hashCode * 397) ^ StagingPercentage.GetHashCode();
            hashCode = (hashCode * 397) ^ (Tag != null ? Tag.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ SHA256.GetHashCode();
            hashCode = (hashCode * 397) ^ Filename.GetHashCode();
            hashCode = (hashCode * 397) ^ Filesize.GetHashCode();
            hashCode = (hashCode * 397) ^ IsDelta.GetHashCode();
            hashCode = (hashCode * 397) ^ Version.GetHashCode();
            hashCode = (hashCode * 397) ^ (OldVersion != null ? OldVersion.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ FileLocation.GetHashCode();
            return hashCode;
        }
    }

    public virtual bool Equals(ReleaseEntry releaseEntry)
    {
        return this.IsDelta == releaseEntry.IsDelta
               && this.Filename == releaseEntry.Filename
               && this.Filesize == releaseEntry.Filesize
               && GetTagEquals(releaseEntry.Tag)
               && this.SHA256 == releaseEntry.SHA256
               && this.StagingPercentage == releaseEntry.StagingPercentage
               && this.OldVersion == releaseEntry.OldVersion
               && this.Version == releaseEntry.Version;
    }

    private bool GetTagEquals(object? otherTag)
    {
        if (Tag is not Array array || otherTag is not Array otherArray)
        {
            return (this.Tag?.Equals(otherTag) ?? this.Tag == otherTag);
        }

        if (array.Length != otherArray.Length)
        {
            return false;
        }
                
        for (int i = 0; i < array.Length; i++)
        {
            var val = array.GetValue(i);
            var otherVal = otherArray.GetValue(i);
            if (!val?.Equals(otherVal) ?? val != otherArray)
            {
                return false;
            }
        }

        return true;
    }
}