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
    /// GitHub client for grabbing updates using their apis!
    /// </summary>
    public class GithubClient : UpdateClient
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
        /// <param name="updateApplier">Update Applier that will apply updates after being downloaded</param>
        /// <param name="organization">Organization that contains the application update files</param>
        /// <param name="repository">Application's repository</param>
        /// <param name="useGraphQl">If we should use <see cref="GithubApiGraphQl"/> (This will require a <see cref="personalToken"/> which has public_repo)</param>
        /// <param name="personalToken">Personal token for accessing the repo if needed</param>
        public GithubClient(
            IUpdateApplier updateApplier,
            string organization,
            string repository,
            bool useGraphQl = false,
            string? personalToken = null)
            : base(updateApplier)
        {
            _organization = organization;
            _repository = repository;
            var canUseGraphQl = useGraphQl && !string.IsNullOrWhiteSpace(personalToken);
            if (!canUseGraphQl)
            {
                Logger.Warning("No personal token was given, going to fall back to REST");
            }

            //Get what api we should use and setup httpClient
            _githubApi = canUseGraphQl ? new GithubApiGraphQl(personalToken, this) : new GithubApiRest(this);
            _httpClient = HttpClientFactory.Create(new HttpClientHandler(), _progressMessageHandler);
        }

        public override Task<UpdateInfo?> CheckForUpdate(bool grabDeltaUpdates = true) => _githubApi.CheckForUpdate(_organization, _repository, grabDeltaUpdates);

        public override Task<ReleaseNote?> GetChangelog(ReleaseEntry entry) => _githubApi.GetChangelog(entry, _organization, _repository);

        public override async Task<bool> DownloadUpdate(ReleaseEntry releaseEntry, Action<double>? progress)
        {
            //If this is the case then we already have the file and it's what we expect, no point in downloading it again
            if (releaseEntry.IsValidReleaseEntry(ApplicationMetadata.ApplicationVersion, true))
            {
                return true;
            }
            
            void ReportProgress(object? sender, HttpProgressEventArgs args)
            {
                progress?.Invoke((double) args.BytesTransferred / releaseEntry.Filesize);
            }

            //Download the file
            Logger.Information("Downloading file {0} ({1})", releaseEntry.Filename, releaseEntry.FileLocation);
            _progressMessageHandler.HttpReceiveProgress += ReportProgress;
            var successfullyDownloaded = await DownloadUpdateInter(releaseEntry, progress);
            _progressMessageHandler.HttpReceiveProgress -= ReportProgress;

            //Check the file
            Logger.Debug("Successfully downloaded {0}", successfullyDownloaded);
            Logger.Information("Checking {0} now it has been downloaded", releaseEntry.Filename);
            if (successfullyDownloaded && releaseEntry.IsValidReleaseEntry(ApplicationMetadata.ApplicationVersion, true))
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

        private async Task<bool> DownloadUpdateInter(ReleaseEntry releaseEntry, Action<double>? progress)
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