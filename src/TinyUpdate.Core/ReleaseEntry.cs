using System;

namespace TinyUpdate.Core
{
    /// <summary>
    /// Details about an update that can be applied to the application
    /// </summary>
    public class ReleaseEntry
    {
        public ReleaseEntry(string sha1, string filename, long filesize, bool isDelta, Version version)
        {
            SHA1 = sha1;
            Filename = filename;
            Filesize = filesize;
            IsDelta = isDelta;
            Version = version;
        }

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
        /// What version this release will bump the application too
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// Reports if this <see cref="ReleaseEntry"/> can be applied
        /// </summary>
        public virtual bool IsValidReleaseEntry()
        {
            throw new NotImplementedException();
        }
    }
}