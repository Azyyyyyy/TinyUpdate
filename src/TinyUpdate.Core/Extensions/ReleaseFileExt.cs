using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SemVersion;
using TinyUpdate.Core.Helper;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Core.Extensions;

/// <summary>
/// Extensions to make using <see cref="ReleaseFile"/> less of a pain
/// </summary>
public static class ReleaseFileExt
{
    private static readonly ILogger Logger = LogManager.CreateLogger(nameof(ReleaseFileExt));

    /// <summary>
    /// Creates a <see cref="ReleaseEntry"/> from <see cref="ReleaseFile"/>'s
    /// </summary>
    /// <param name="releaseFiles">Release files</param>
    /// <param name="folderLocation">Where the release will be located</param>
    /// <param name="tag">Tag to use for any extra data (Normally the tag that is linked to a <see cref="ReleaseFile"/> in services)</param>
    public static IEnumerable<ReleaseEntry> ToReleaseEntries(
        this IEnumerable<ReleaseFile> releaseFiles,
        string folderLocation,
        object? tag = null)
    {
        foreach (var releaseFile in releaseFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(releaseFile.Name);
            var version = fileName.ToVersion();
            if (version == null)
            {
                Logger.Log(Level.Warn ,$"We can't grab the version that {fileName} is from the filename, skipping...");
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

    public static UpdateInfo? GetUpdateInfo(this FileInfo fileInfo, ApplicationMetadata metadata, bool grabDeltaUpdates, object? tag = null, string? folderLocation = null)
    {
        return GetUpdateInfo(fileInfo.FullName, metadata, grabDeltaUpdates, tag, folderLocation);
    }
        
    public static UpdateInfo? GetUpdateInfo(string fileLocation, ApplicationMetadata metadata, bool grabDeltaUpdates, object? tag = null, string? folderLocation = null)
    {
        if (!File.Exists(fileLocation))
        {
            Logger.Error("{0} doesn't exist, can't get UpdateInfo", fileLocation);
            return null;
        }
            
        return new UpdateInfo(metadata.ApplicationVersion,
            ReleaseFile.ReadReleaseFile(File.ReadLines(fileLocation))
                .ToReleaseEntries(folderLocation ?? metadata.TempFolder, tag)
                .FilterReleases(metadata.ApplicationFolder, grabDeltaUpdates, metadata.ApplicationVersion).ToArray());
    }
        
    /// <summary>
    /// Checks that the <see cref="ReleaseEntry"/> can be used
    /// </summary>
    public static bool CheckReleaseEntry(this ReleaseEntry releaseEntry, SemanticVersion applicationVersion, bool successfullyDownloaded)
    {
        Logger.Log(Level.Info ,$"Checking {releaseEntry.Filename}");
        if (successfullyDownloaded && releaseEntry.IsValidReleaseEntry(applicationVersion, true))
        {
            return true;
        }
                        
        Logger.Error("Checking file {0} failed after downloading, going to delete it to be safe", releaseEntry.Filename);
        try
        {
            File.Delete(releaseEntry.FileLocation);
        }
        catch (Exception e)
        {
            Logger.Log(e);
        }
        return false;
    }
        
    /// <summary>
    /// Gets rid of any unneeded releases
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
                    
                //TODO: See why this is matching when they is nothing there
                //If we have the OS in the filename then also check that
                var dashIndex = x.Filename.LastIndexOf('-');
                var match = VersionExt.OsRegex.Match(x.Filename, dashIndex > -1 ? dashIndex : 0);
                if (match.Success && !string.IsNullOrWhiteSpace(match.Value))
                {
                    return shouldProcess && OSPlatform.Create(match.Value[1..]) == OSHelper.ActiveOS;
                }
                return shouldProcess;
            });
    }
}