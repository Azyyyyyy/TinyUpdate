using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Github
{
    /// <summary>
    /// Base class to use when making a Api to talk with Github
    /// </summary>
    public abstract class GithubApi
    {
        protected readonly HttpClient HttpClient;
        protected readonly ILogging Logger;
        
        /// <summary>
        /// Api constructor
        /// </summary>
        /// <param name="apiEndpoint">Base endpoint to use</param>
        protected GithubApi(string apiEndpoint)
        {
            Logger = LoggingCreator.CreateLogger(GetType().Name);
            HttpClient = new HttpClient
            {
                BaseAddress = new Uri(apiEndpoint)
            };
            HttpClient.DefaultRequestHeaders.Add("User-Agent", $"TinyUpdate-{Global.ApplicationName}-{Global.ApplicationVersion}");
        }


        /// <inheritdoc cref="UpdateClient.CheckForUpdate()"/>
        /// <param name="organization">The organization that owns the <see cref="repository"/></param>
        /// <param name="repository">The repository name</param>
        public abstract Task<UpdateInfo?> CheckForUpdate(string organization, string repository, bool grabDeltaUpdates);

        /// <summary>Gets the changelog for a certain <see cref="ReleaseEntry"/></summary>
        /// <param name="entry">The repository name</param>
        /// <inheritdoc cref="CheckForUpdate"/>
        public abstract Task<ReleaseNote?> GetChangelog(ReleaseEntry entry, string organization, string repository);
        
        /// <summary>
        /// Downloads the release file and creates the <see cref="UpdateInfo"/>
        /// </summary>
        /// <param name="tagName">What the tag that contains the RELEASE file is</param>
        /// <param name="fileSize">How big the RELEASE file should be</param>
        /// <param name="downloadUrl">Where the RELEASE file is</param>
        protected async Task<UpdateInfo?> DownloadAndParseReleaseFile(string tagName, long fileSize, string downloadUrl, bool grabDeltaUpdates)
        {
            //Download the RELEASE file if we don't already have it
            var releaseFileLoc = Path.Combine(Global.TempFolder, $"RELEASES-{Global.ApplicationName}-{tagName}");
            long? fileLength = null;
            if (!File.Exists(releaseFileLoc))
            {
                Directory.CreateDirectory(Global.TempFolder);
                using var releaseStream = await HttpClient.GetStreamAsync(downloadUrl);
                using var releaseFileStream = File.Open(releaseFileLoc, FileMode.CreateNew, FileAccess.ReadWrite);
                await releaseStream.CopyToAsync(releaseFileStream);
                fileLength = releaseFileStream.Length;
            }
            fileLength ??= new FileInfo(releaseFileLoc).Length;
            
            //Just do a sanity check of the file
            if (fileLength != fileSize)
            {
                Logger.Error("RELEASE file isn't the length as expected, deleting and returning null...");
                File.Delete(releaseFileLoc);
                return null;
            }
            
            //Create the UpdateInfo
            return new UpdateInfo(
                ReleaseFile.ReadReleaseFile(File.ReadLines(releaseFileLoc))
                    .ToReleaseEntries(tagName)
                    .FilterReleases(grabDeltaUpdates));
        }
    }
}