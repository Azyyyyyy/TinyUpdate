using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Update;

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
        private ApplicationMetadata _applicationMetadata => _githubClient.ApplicationMetadata;

        /// <summary>
        /// Api constructor
        /// </summary>
        /// <param name="githubClient">Client that owns this Api</param>
        /// <param name="apiEndpoint">Base endpoint to use</param>
        protected GithubApi(GithubClient githubClient, string apiEndpoint)
        {
            _githubClient = githubClient ?? throw new ArgumentNullException(nameof(githubClient));
            Logger = LoggingCreator.CreateLogger(GetType().Name);
            HttpClient = new HttpClient
            {
                BaseAddress = new Uri(apiEndpoint)
            };
            HttpClient.DefaultRequestHeaders.Add("User-Agent", $"TinyUpdate-{_applicationMetadata.ApplicationName}-{_applicationMetadata.ApplicationVersion}");
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
            //Download the RELEASE file if we don't already have it
            var releaseFileLoc = Path.Combine(_applicationMetadata.TempFolder, $"RELEASES-{_applicationMetadata.ApplicationName}-{tagName}");
            long? fileLength = null;
            if (!File.Exists(releaseFileLoc))
            {
                Directory.CreateDirectory(_applicationMetadata.TempFolder);
                var response = await GetResponseMessage(new HttpRequestMessage(HttpMethod.Get, downloadUrl));
                if (response == null)
                {
                    Logger.Error("Didn't get anything from Github, can't download");
                    return null;
                }
                
                using var releaseStream = await response.Content.ReadAsStreamAsync();
                using var releaseFileStream = File.Open(releaseFileLoc, FileMode.CreateNew, FileAccess.ReadWrite);
                await releaseStream.CopyToAsync(releaseFileStream);
                fileLength = releaseFileStream.Length;
            }
            fileLength ??= new FileInfo(releaseFileLoc).Length;
            
            //Just do a sanity check of the file size
            if (fileLength != fileSize)
            {
                Logger.Error("RELEASE file isn't the length as expected, deleting and returning null...");
                File.Delete(releaseFileLoc);
                return null;
            }
            
            //Create the UpdateInfo
            return new UpdateInfo(_applicationMetadata.ApplicationVersion,
                ReleaseFile.ReadReleaseFile(File.ReadLines(releaseFileLoc))
                    .ToReleaseEntries(tagName)
                    .FilterReleases(grabDeltaUpdates, _applicationMetadata.ApplicationVersion).ToArray());
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

            HttpResponseMessage? response;
            try
            {
                response = await HttpClient.SendAsync(requestMessage);
            }
            catch (Exception e)
            {
                Logger.Error(e);
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