using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Update;
using TinyUpdate.Github.GraphQl;
using TinyUpdate.Github.Rest;
using TinyUpdate.Http;

namespace TinyUpdate.Github
{
    /// <summary>
    /// GitHub client for grabbing updates using their apis!
    /// </summary>
    public class GithubUpdateClient : HttpUpdateClient
    {
        private readonly string _organization;
        private readonly string _repository;
        private readonly GithubApi _githubApi;
        
        /// <summary>
        /// Creates a github client
        /// </summary>
        /// <param name="updateApplier">Update Applier that will apply updates after being downloaded</param>
        /// <param name="organization">Organization that contains the application update files</param>
        /// <param name="repository">Application's repository</param>
        /// <param name="useGraphQl">If we should use <see cref="GithubApiGraphQl"/> (This will require a <see cref="personalToken"/> which has public_repo)</param>
        /// <param name="personalToken">Personal token for accessing the repo if needed</param>
        public GithubUpdateClient(
            IUpdateApplier updateApplier,
            string organization,
            string repository,
            bool useGraphQl = false,
            string? personalToken = null)
            : base("https://blank.org", updateApplier, NoteType.Markdown)
        {
            _organization = organization;
            _repository = repository;
            var canUseGraphQl = useGraphQl && !string.IsNullOrWhiteSpace(personalToken);
            if (!canUseGraphQl)
            {
                Logger.Warning("No personal token was given, going to fall back to REST");
            }

            //Get what api we should use and setup httpClient
            _githubApi = canUseGraphQl ? new GithubApiGraphQl(personalToken!, _httpClient, this) : new GithubApiRest(this, _httpClient);
        }

        public override Task<UpdateInfo?> CheckForUpdate(bool grabDeltaUpdates = true) => _githubApi.CheckForUpdate(_organization, _repository, grabDeltaUpdates);

        public override Task<ReleaseNote?> GetChangelog(ReleaseEntry entry) => _githubApi.GetChangelog(entry, _organization, _repository);

        protected override string GetUriForReleaseEntry(ReleaseEntry releaseEntry) =>
            $"https://github.com/{_organization}/{_repository}/releases/download/{releaseEntry.Tag}/{releaseEntry.Filename}";

        //We need this to be seen by GithubApi but don't want the end user to use it
        internal new Task<long> DownloadReleaseFile(string releaseFileLocation, string downloadUrl) =>
            base.DownloadReleaseFile(releaseFileLocation, downloadUrl);
    }
}