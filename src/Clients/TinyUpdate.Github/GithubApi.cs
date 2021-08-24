using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Update;
using TinyUpdate.Http.Extensions;

namespace TinyUpdate.Github
{
    /// <summary>
    /// Base class for talking to github and understanding what is used
    /// </summary>
    public abstract class GithubApi
    {
        protected readonly HttpClient HttpClient;
        protected readonly ILogging Logger;
        private readonly GithubClient _githubClient;
        private ApplicationMetadata ApplicationMetadata => _githubClient.ApplicationMetadata;

        /// <summary>
        /// Api constructor
        /// </summary>
        /// <param name="githubClient">Client that owns this Api</param>
        /// <param name="apiEndpoint">Base endpoint to use</param>
        protected GithubApi(GithubClient githubClient, HttpClient httpClient, string apiEndpoint)
        {
            _githubClient = githubClient ?? throw new ArgumentNullException(nameof(githubClient));
            Logger = LoggingCreator.CreateLogger(GetType().Name);

            HttpClient = httpClient;
            HttpClient.BaseAddress = new Uri(apiEndpoint);
            HttpClient.DefaultRequestHeaders.Add("User-Agent", $"TinyUpdate-{ApplicationMetadata.ApplicationName}-{ApplicationMetadata.ApplicationVersion}");
        }


        /// <inheritdoc cref="UpdateClient.CheckForUpdate"/>
        /// <param name="organization">The organization that owns the <see cref="repository"/></param>
        /// <param name="repository">The repository name</param>
        // ReSharper disable once InvalidXmlDocComment
        public abstract Task<UpdateInfo?> CheckForUpdate(string organization, string repository, bool grabDeltaUpdates);

        /// <inheritdoc cref="UpdateClient.GetChangelog(ReleaseEntry)"/>
        /// <param name="organization">The organization that owns the <see cref="repository"/></param>
        /// <param name="repository">The repository name</param>
        // ReSharper disable once InvalidXmlDocComment
        public abstract Task<ReleaseNote?> GetChangelog(ReleaseEntry entry, string organization, string repository);
        
        /// <summary>
        /// Downloads the release file and creates the <see cref="UpdateInfo"/>
        /// </summary>
        /// <param name="tagName">What the tag that contains the RELEASE file is</param>
        /// <param name="fileSize">How big the RELEASE file should be</param>
        /// <param name="downloadUrl">The URI with the the RELEASE file</param>
        /// <param name="grabDeltaUpdates">If we want to grab only delta updates from the RELEASE file (If false we only grab full update files)</param>
        protected async Task<UpdateInfo?> DownloadAndParseReleaseFile(string tagName, long fileSize, string downloadUrl, bool grabDeltaUpdates)
        {
            var releaseFileLocation = Path.Combine(ApplicationMetadata.TempFolder,
                $"RELEASES-{ApplicationMetadata.ApplicationName}-{tagName}");
            var fileLength = await _githubClient.DownloadReleaseFile(releaseFileLocation, downloadUrl);
            
            //Just do a sanity check of the file size
            if (fileLength != fileSize)
            {
                Logger.Error("RELEASE file isn't the length as expected, deleting and returning null...");
                File.Delete(releaseFileLocation);
                return null;
            }
            
            //Create the UpdateInfo
            return ReleaseFileExt.GetUpdateInfo(releaseFileLocation, ApplicationMetadata, grabDeltaUpdates, tagName);
        }

        private DateTime? _rateLimitTime;
        protected async Task<HttpResponseMessage?> GetResponseMessage(HttpRequestMessage requestMessage)
        {
            if (_rateLimitTime != null)
            {
                if (_rateLimitTime > DateTime.Now)
                {
                    Logger.Warning("We are still in the rate limit timeframe (Resets at {0}), not going to try this request", _rateLimitTime);
                    return null;
                }
                //Null it if we no-longer are in the rate limit timeframe
                _rateLimitTime = null;
            }

            var response = await HttpClient.GetResponseMessage(requestMessage);
            if (response == null)
            {
                return null;
            }

            //Check that we got something from it
            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            var rateLimit = await GetRateLimitTime(response);
            if (rateLimit.IsRateLimited)
            {
                Logger.Error("We are being rate limited! This will reset at {0}", rateLimit.ResetTime!.Value);
                _rateLimitTime = rateLimit.ResetTime.Value;
                return null;
            }
            
            //Report based on the status code as it might show what the user/Api has done
            Logger.Error("Github returned an unsuccessful status code ({0})", response.StatusCode);
            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    Logger.Error("We detected that the status code was 401; have you given a valid personal token? (You need the token to have public_repo)");
                    break;
                case HttpStatusCode.NotFound:
                    Logger.Error("We detected that the status code was 404; did you misspell or not give a token for accessing a private repo?");
                    break;
            }

            return null;
        }

        protected abstract Task<RateLimit> GetRateLimitTime(HttpResponseMessage responseMessage);
    }
}