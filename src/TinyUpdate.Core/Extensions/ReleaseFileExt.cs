using System.Collections.Generic;
using System.IO;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Utils;

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
        /// <param name="tag">Tag to use for any extra data (Normally the tag that is linked to a <see cref="ReleaseFile"/> in services)</param>
        public static IEnumerable<ReleaseEntry> ToReleaseEntries(this IEnumerable<ReleaseFile> releaseFiles, string? tag = null)
        {
            foreach (var releaseFile in releaseFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(releaseFile.Name);
                var version = fileName.ToVersion();
                if (version == null)
                {
                    Logger.Warning("we wasn't able to get a version from the filename {0}, skipping...", fileName);
                    continue;
                }
                
                yield return new
                    ReleaseEntry(
                        releaseFile.SHA256, 
                        releaseFile.Name,
                        releaseFile.Size, 
                        fileName.EndsWith("-delta"),
                        version,
                        oldVersion: releaseFile.OldVersion, 
                        tag: tag);
            }
        }
    }
}