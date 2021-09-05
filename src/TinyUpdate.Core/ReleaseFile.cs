using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SemVersion;
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

        public ReleaseFile(string sha256, string name, long size, int? stagingPercentage, SemanticVersion? oldVersion = null)
        {
            SHA256 = sha256;
            Name = name;
            Size = size;
            StagingPercentage = stagingPercentage;
            OldVersion = oldVersion;
        }

        public int? StagingPercentage { get; }

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
        public SemanticVersion? OldVersion { get; }

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
                   && SHA256 == otherReleaseFile.SHA256
                   && StagingPercentage == otherReleaseFile.StagingPercentage;
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
            var oldReleases = Array.Empty<ReleaseFile>();
            if (file.Exists)
            {
                Logging.Warning("{0} already exists, grabbing all valid releases and recreating file", fileLocation);
                oldReleases = ReadReleaseFile(File.ReadLines(fileLocation)).ToArray();
                file.Delete();
            }

            using var textFileStream = file.CreateText();
            foreach (var releaseFile in releaseFiles.Concat(oldReleases))
            {
                await textFileStream.WriteLineAsync(releaseFile.ToString());
            }

            return true;
        }

        public override string ToString()
        {
            var s = $"{SHA256} {Name} {Size}";
            if (OldVersion != null)
            {
                s += " " + OldVersion;
            }
            if (StagingPercentage.HasValue)
            {
                s += " " + StagingPercentage;
            }
            return s;
        }

        /// <summary>
        /// Reads a chunk of lines and make them into <see cref="ReleaseFile"/>
        /// </summary>
        /// <param name="lines">Lines to make into <see cref="ReleaseFile"/></param>
        public static IEnumerable<ReleaseFile> ReadReleaseFile(IEnumerable<string> lines)
        {
            //This is what the input should be like
            //1. {hash} appname-29.2.4-delta.{EXTENSION} {filesize}
            //2. {hash} appname-29.2.4-delta.{EXTENSION} {filesize} {old version}
            //3. Same as 1/2 but with ' {staging percentage}'
            foreach (var line in lines)
            {
                var re = MakeReleaseFile(line, out var successful);
                if (successful)
                {
                    yield return re!;
                }
            }
        }

        private static ReleaseFile? MakeReleaseFile(string line, out bool successful)
        {
            successful = false;
            var lineS = line.Split(' ');
            /*Check that the line only has 3/5 lines, if not then
             that means it's not a release file for sure*/
            if (lineS.Length is < 3 or > 5)
            {
                return null;
            }

            var sha256 = lineS[0];
            var fileName = lineS[1];
            SemanticVersion? oldVersion = null;
            int stagingPercentage = 0;

            var hasFilesize = long.TryParse(lineS[2], out var fileSize);
            var hasOldVersion = lineS.Length > 3 && SemanticVersion.TryParse(lineS[3], out oldVersion);
            var hasStagingPercentage = lineS.Length > 3 && int.TryParse(lineS[^1], out stagingPercentage);
                
            if (hasFilesize &&
                SHA256Util.IsValidSHA256(sha256))
            {
                successful = true;
                return new ReleaseFile(
                    sha256, 
                    fileName, 
                    fileSize, 
                    hasStagingPercentage ? stagingPercentage : null,
                    oldVersion);
                
            }

            //If we got here then we wasn't able to create a release file from the data given
            Logging.Warning("Line {0} is not a valid ReleaseFile", line);
            return null;
        }
    }
}