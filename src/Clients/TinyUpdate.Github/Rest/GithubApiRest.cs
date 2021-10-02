using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Github.Rest
{
    /// <summary>
    /// <see cref="GithubApi"/> that uses Github's REST api (V3)
    /// </summary>
    public class GithubApiRest : GithubApi
    {
        public GithubApiRest(GithubUpdateClient githubClient, HttpClient httpClient)
            : base(githubClient, httpClient, "https://api.github.com") { }
        
        public override async Task<UpdateInfo?> CheckForUpdate(string organization, string repository, bool grabDeltaUpdates)
        {
            //Get release data
            var releases = await GetGithubRelease(organization, repository);
            if (releases == null)
            {
                Logger.Error("We didn't get any files reported back from github");
                return null;
            }

            //Check that we got a release file from the response
            var release = releases.Assets.FirstOrDefault(x => x.Name == "RELEASES");
            if (release == null)
            {
                Logger.Error("We can't find any RELEASES file in the newest github release");
                return null;
            }

            //Download the release file for us to go through it!
            Logger.Information("RELEASES file found in newest github release, downloading if it doesn't yet exist on disk");
            return await DownloadAndParseReleaseFile(releases.TagName, release.Size, release.BrowserDownloadUrl, grabDeltaUpdates);
        }

        public override async Task<ReleaseNote?> GetChangelog(ReleaseEntry entry, string organization, string repository)
        {
            var releases = await GetGithubRelease(organization, repository);
            return string.IsNullOrWhiteSpace(releases?.Body) ? 
                null :
                new ReleaseNote(releases!.Body, NoteType.Markdown);
        }

        protected override Task<RateLimit> GetRateLimitTime(HttpResponseMessage responseMessage)
        {
            var remainingCalls = responseMessage.Headers.FirstOrDefault(x => x.Key == "X-RateLimit-Remaining");
            var rateLimitTime = responseMessage.Headers.FirstOrDefault(x => x.Key == "X-RateLimit-Reset");
            //If this is the case then we aren't being rate limited (at least not being reported to us anyway)
            if (!int.TryParse(remainingCalls.Value.First(), out var remaining) 
                || remaining != 0)
            {
                return Task.FromResult(new RateLimit(false));
            }
            
            if (long.TryParse(rateLimitTime.Value.First(), out var offset))
            {
                return Task.FromResult(new RateLimit(true, DateTimeOffset.FromUnixTimeSeconds(offset).DateTime));
            }

            return Task.FromResult(new RateLimit(false));
        }


        private async Task<GithubReleaseRest?> GetGithubRelease(string organization, string repository)
        {
            //Make request for getting the newest update and check that we got something from it
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/repos/{organization}/{repository}/releases/latest");
            using var response = await GetResponseMessage(request);
            if (response == null)
            {
                Logger.Error("Didn't get anything from Github");
                return null;
            }

            if (response.IsSuccessStatusCode)
            {
                Logger.Information("Got response, reading response...");
                return await JsonSerializer.DeserializeAsync<GithubReleaseRest>(await response.Content.ReadAsStreamAsync());
            }

            Logger.Error("Github returned an unsuccessful status code ({0})", response.StatusCode);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Logger.Error("We detected that the status code was 401, have you given an valid token (if it's a private repo) or misnamed the repo?");
            }
            return null;
        }
    }
}