using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Core
{
    /// <summary>
    /// Class that contains all the information we know from the RELEASE file
    /// </summary>
    public class ReleaseFile
    {
        private static readonly ILogging Logging = LoggingCreator.CreateLogger(nameof(ReleaseFile));

        public ReleaseFile(string sha256, string name, long size, Version? oldVersion = null)
        {
            SHA256 = sha256;
            Name = name;
            Size = size;
            OldVersion = oldVersion;
        }

        /// <summary>
        /// Hash that the downloaded file should be
        /// </summary>
        public string SHA256 { get; }

        /// <summary>
        /// The name of the release file
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The size of the file when downloaded
        /// </summary>
        public long Size { get; }

        /// <summary>
        /// The version that this update will be coming from
        /// </summary>
        public Version? OldVersion { get; }

        public override bool Equals(object obj)
        {
            return obj is ReleaseFile otherReleaseFile
                   && Equals(otherReleaseFile);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = SHA256.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ Size.GetHashCode();
                hashCode = (hashCode * 397) ^ (OldVersion != null ? OldVersion.GetHashCode() : 0);
                return hashCode;
            }
        }

        public bool Equals(ReleaseFile otherReleaseFile)
        {
            return Name == otherReleaseFile.Name
                   && Size == otherReleaseFile.Size
                   && OldVersion == otherReleaseFile.OldVersion
                   && SHA256 == otherReleaseFile.SHA256;
        }

        /// <summary>
        /// Creates a RELEASE file 
        /// </summary>
        /// <param name="releaseFiles">All the release data to put into the RELEASE file</param>
        /// <param name="fileLocation">Where the file should be located (Do not give a filename, we add that ourselves)</param>
        /// <returns>If we was able to create a RELEASE file</returns>
        public static async Task<bool> CreateReleaseFile(IEnumerable<ReleaseFile> releaseFiles, string fileLocation)
        {
            if (!Directory.Exists(fileLocation))
            {
                Logging.Error("Directory {0} doesn't exist, failing...", fileLocation);
                return false;
            }
            
            fileLocation = Path.Combine(fileLocation, "RELEASE");
            var file = new FileInfo(fileLocation);
            if (file.Exists)
            {
                Logging.Warning("{0} already exists, going to delete it and recreate it", fileLocation);
                file.Delete();
            }

            using var textFileStream = file.CreateText();
            foreach (var releaseFile in releaseFiles)
            {
                await textFileStream.WriteLineAsync(
                    $"{releaseFile.SHA256} {releaseFile.Name} {(releaseFile.OldVersion != null ? $"{releaseFile.OldVersion} " : "")}{releaseFile.Size}");
            }

            return true;
        }

        /// <summary>
        /// Reads a chunk of lines and make them into <see cref="ReleaseFile"/>
        /// </summary>
        /// <param name="lines">Lines to make into <see cref="ReleaseFile"/></param>
        public static IEnumerable<ReleaseFile> ReadReleaseFile(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                var lineS = line.Split(' ');
                /*Check that the line only has 3/4 lines, if not then
                 that means it's not a release file for sure*/
                if (lineS.Length is < 3 or > 4)
                {
                    continue;
                }

                var sha256 = lineS[0];
                var fileName = lineS[1];
                Version? oldVersion = null;
                if (long.TryParse(lineS[^1], out var fileSize)
                    && SHA256Util.IsValidSHA256(sha256)
                    && (lineS.Length != 4 || Version.TryParse(lineS[2], out oldVersion)))
                {
                    yield return new ReleaseFile(sha256, fileName, fileSize, oldVersion);
                    continue;
                }

                //If we got here then we wasn't able to create a release file from the data given
                Logging.Warning("Line {0} is not a valid ReleaseFile", line);
            }
        }
    }
}