using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Update;
using TinyUpdate.Github.GraphQL;
using TinyUpdate.Github.Rest;

namespace TinyUpdate.Github
{
    /// <summary>
    /// GitHub client to get any updates that need to be applied
    /// </summary>
    public class GithubClient : UpdateChecker
    {
        private readonly string _organization;
        private readonly string _repository;
        private readonly GithubApi _githubApi;
        
        //Need this to get the process of the http client
        private readonly ProgressMessageHandler _progressMessageHandler = new();
        private readonly HttpClient _httpClient;
        
        /// <summary>
        /// Creates a github client
        /// </summary>
        /// <param name="updateApplier"></param>
        /// <param name="organization">Organization that contains the application code</param>
        /// <param name="repository">Application's repository</param>
        /// <param name="useGraphQL">If we want to the <see cref="GithubApiGraphQL"/> for grabbing data from github (Will require <see cref="personalToken"/> which has public_repo)</param>
        /// <param name="personalToken">Personal token is using <see cref="GithubApiGraphQL"/></param>
        public GithubClient(
            IUpdateApplier updateApplier,
            string organization,
            string repository,
            bool useGraphQL = false,
            string? personalToken = null)
            : base(updateApplier)
        {
            _organization = organization;
            _repository = repository;
            var canUseGraphQL = !string.IsNullOrWhiteSpace(personalToken);
            if (useGraphQL && !canUseGraphQL)
            {
                Logger.Warning("No personal token was given, going to fall back to REST");
            }
            
            _githubApi = useGraphQL && canUseGraphQL ? new GithubApiGraphQL(personalToken) : new GithubApiRest();
            _httpClient = HttpClientFactory.Create(new HttpClientHandler(), _progressMessageHandler);
        }

        public override Task<UpdateInfo?> CheckForUpdate() => _githubApi.CheckForUpdate(_organization, _repository);

        public override Task<ReleaseNote?> GetChangelog(ReleaseEntry entry) => _githubApi.GetChangelog(entry, _organization, _repository);

        public override async Task<bool> DownloadUpdate(ReleaseEntry releaseEntry, Action<decimal>? progress)
        {
            //If this is the case then we already have the file and it's what we expect, no point in downloading it again
            if (releaseEntry.IsValidReleaseEntry(true))
            {
                return true;
            }
            
            var requestToCheck = $"response-content-disposition=attachment%3B%20filename%3D{releaseEntry.Filename}";
            void ReportProgress(object sender, HttpProgressEventArgs args)
            {
                if (sender is HttpRequestMessage message
                    && message.RequestUri.Query.Contains(requestToCheck))
                {
                    progress?.Invoke((decimal) args.BytesTransferred / releaseEntry.Filesize);
                }
            }

            //Download the file
            Logger.Information("Downloading file {0} ({1})", releaseEntry.Filename, releaseEntry.FileLocation);

            _progressMessageHandler.HttpReceiveProgress += ReportProgress;
            var successfullyDownloaded = await DownloadUpdateInter(releaseEntry, progress);
            _progressMessageHandler.HttpReceiveProgress -= ReportProgress;

            //Check the file
            Logger.Debug("successfullyDownloaded: {0}", successfullyDownloaded);
            Logger.Information("Checking {0} now it should be downloaded", releaseEntry.Filename);
            if (successfullyDownloaded && releaseEntry.IsValidReleaseEntry(true))
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
                Logger.Error(e);
            }
            return false;
        }

        private async Task<bool> DownloadUpdateInter(ReleaseEntry releaseEntry, Action<decimal>? progress)
        {
            try
            {
                using var releaseStream = await _httpClient.GetStreamAsync($"https://github.com/{_organization}/{_repository}/releases/download/{releaseEntry.Tag}/{releaseEntry.Filename}");

                //Delete the file if it already exists
                if (File.Exists(releaseEntry.FileLocation))
                {
                    Logger.Warning("{0} already exists, going to delete it", releaseEntry.FileLocation);
                    File.Delete(releaseEntry.FileLocation);
                }
            
                using var releaseFileStream = File.Open(releaseEntry.FileLocation, FileMode.CreateNew, FileAccess.ReadWrite);
                await releaseStream.CopyToAsync(releaseFileStream);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }
    }
}