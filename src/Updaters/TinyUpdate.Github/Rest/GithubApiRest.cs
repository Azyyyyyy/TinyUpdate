using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Update;
using TinyUpdate.Github.GraphQL;

namespace TinyUpdate.Github.Rest
{
    public class GithubApiRest : GithubApi
    {
        public GithubApiRest() : base("https://api.github.com")
        {
        }
        
        public override async Task<UpdateInfo?> CheckForUpdate(string organization, string repository)
        {
            var releases = await GetGithubReleaseRest(organization, repository);
            if (releases == null)
            {
                Logger.Error("We can't use what was returned from GitHub API");
                return null;
            }

            //Check that we got a release file from the response
            var release = releases.Assets.FirstOrDefault(x => x.Name == "RELEASES");
            if (release == null)
            {
                Logger.Error("We can't find any RELEASES file in the newest github release");
                return null;
            }
            Logger.Information("RELEASES file exists in newest github release, downloading if not already downloaded");

            return await DownloadAndParseReleaseFile(releases.TagName, release.Size, release.BrowserDownloadUrl);
        }

        public override async Task<ReleaseNote?> GetChangelog(ReleaseEntry entry, string organization, string repository)
        {
            var releases = await GetGithubReleaseRest(organization, repository);
            return string.IsNullOrWhiteSpace(releases?.Body) ? 
                null : 
                new ReleaseNote(
                    releases?.Body, 
                    NoteType.Markdown);
        }
        
        
        private async Task<GithubReleaseRest?> GetGithubReleaseRest(string organization, string repository)
        {
            //TODO: Handle errors
            //TODO: Handle when we get rate limited
            //TODO: Add something to not crash when we have no wifi
            
            //Make request for getting the newest update and
            //Check that we got something from it
            using var response = await HttpClient.GetAsync($"/repos/{organization}/{repository}/releases/latest");
            if (response.IsSuccessStatusCode)
            {
                return await JsonSerializer.DeserializeAsync<GithubReleaseRest>(await response.Content.ReadAsStreamAsync());
            }

            Logger.Error("Github returned an unsuccessful status code ({0})", response.StatusCode);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Logger.Error("We detected that the status code was 401, have you given an valid personal token? (You need the token to have public_repo)");
            }

            return null;
        }
    }
}