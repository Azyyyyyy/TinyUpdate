using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SemVersion;
using TinyUpdate.Core.Helper;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Core.Extensions
{
    /// <summary>
    /// Extensions to make using <see cref="ReleaseFile"/> less of a pain
    /// </summary>
    public static class ReleaseFileExt
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(ReleaseFileExt));

        /// <summary>
        /// Creates a <see cref="ReleaseEntry"/> from <see cref="ReleaseFile"/>'s
        /// </summary>
        /// <param name="releaseFiles">Release files</param>
        /// <param name="folderLocation">Where the release will be located</param>
        /// <param name="tag">Tag to use for any extra data (Normally the tag that is linked to a <see cref="ReleaseFile"/> in services)</param>
        public static IEnumerable<ReleaseEntry> ToReleaseEntries(
            this IEnumerable<ReleaseFile> releaseFiles,
            string folderLocation,
            string? tag = null)
        {
            foreach (var releaseFile in releaseFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(releaseFile.Name);
                var version = fileName.ToVersion();
                if (version == null)
                {
                    Logger.Warning("We can't grab the version that {0} is from the filename, skipping...", fileName);
                    continue;
                }

                yield return new
                    ReleaseEntry(
                        releaseFile.SHA256,
                        releaseFile.Name,
                        releaseFile.Size,
                        VersionExt.OsRegex.Replace(fileName, string.Empty).EndsWith("-delta"),
                        version,
                        folderLocation,
                        releaseFile.OldVersion,
                        tag,
                        releaseFile.StagingPercentage);
            }
        }

        /// <summary>
        /// Get's rid of any unneeded releases
        /// </summary>
        /// <param name="releaseFiles">Release files to filter through</param>
        /// <param name="applicationLocation">Where the application is stored</param>
        /// <param name="haveDelta">If we want to grab delta updates or </param>
        /// <param name="applicationVersion">What version the application is currently on</param>
        public static IEnumerable<ReleaseEntry> FilterReleases(
            this IEnumerable<ReleaseEntry> releaseFiles, 
            string applicationLocation,
            bool haveDelta,
            SemanticVersion applicationVersion)
        {
            var userId = Staging.GetOrCreateStagedUserId(applicationLocation);
            return releaseFiles
                .Where(x =>
                {
                    var shouldProcess = x.IsDelta == haveDelta
                            && x.Version > applicationVersion
                            && x.IsStagingMatch(userId);
                    
                    //If we have the OS in the filename then also check that
                    var dashIndex = x.Filename.LastIndexOf('-');
                    var match = VersionExt.OsRegex.Match(x.Filename, dashIndex > -1 ? dashIndex : 0);
                    if (match.Success)
                    {
                        return shouldProcess && OSPlatform.Create(match.Value[1..]) == OSHelper.ActiveOS;
                    }
                    return shouldProcess;
                });
        }
    }
}