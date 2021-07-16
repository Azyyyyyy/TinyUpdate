using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Github.GraphQL
{
    /// <summary>
    /// <see cref="GithubApi"/> that uses the GraphQL API (V4). This requires a personal token with public_repo
    /// </summary>
    // ReSharper disable once InconsistentNaming
    internal class GithubApiGraphQL : GithubApi
    {
        public GithubApiGraphQL(string personalToken)
            : base("https://api.github.com/graphql")
        {
            //Personal token needs to be base64
            var basicValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(personalToken));
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicValue);
        }

        private const string ChangeLogQuery = 
@"query($org: String!, $repo: String!)
{
  organization(login: $org) {
    repository(name: $repo) {
      releases(last: 1){
        nodes{
          description
        }
      }
    }
  }
}";
        
        private const string UpdateQuery = 
@"query($org: String!, $repo: String!)
{
  organization(login: $org) {
    repository(name: $repo) {
      releases(last: 1){
        nodes{
          tagName
          releaseAssets(name: ""RELEASES"", last: 1){
            nodes{
              downloadUrl
              size
              name
            }
          }
        }
      }
    }
  }
}";

        /// <see cref="UpdateClient.CheckForUpdate(bool)"/>
        /// <param name="organization">The organization name</param>
        /// <param name="repository">The repository that contains the update files</param>
        /// <param name="grabDeltaUpdates">If we only grab delta files (If false only full update files)</param>
        public override async Task<UpdateInfo?> CheckForUpdate(string organization, string repository, bool grabDeltaUpdates)
        {
            var releases = await GetGithubRelease(UpdateQuery, organization, repository);
            if (releases == null)
            {
                Logger.Error("We can't use what was returned from GitHub API");
                return null;
            }

            //Check that a release file is contained in the response
            var release = releases.Data.Organization.Repository.Releases.Nodes.FirstOrDefault();
            if (release == null)
            {
                Logger.Error("We didn't get any files reported back from github");
                return null;
            }
            var releaseFile = release.ReleaseAssets.Nodes.FirstOrDefault(x => x.Name != "RELEASES");
            if (releaseFile == null)
            {
                Logger.Error("We can't find any RELEASES file in the newest github release");
                return null;
            }

            Logger.Information("RELEASES file found in newest github release, downloading if it doesn't yet exist on disk");
            return await DownloadAndParseReleaseFile(release.TagName, releaseFile.Size, releaseFile.DownloadUrl, grabDeltaUpdates);
        }

        public override async Task<ReleaseNote?> GetChangelog(ReleaseEntry entry, string organization, string repository)
        {
            var releases = await GetGithubRelease(ChangeLogQuery, organization, repository);
            if (releases == null)
            {
                Logger.Error("We can't use what was returned from GitHub API");
                return null;
            }

            var disc = releases.Data.Organization.Repository.Releases.Nodes.First().Description;
            return string.IsNullOrWhiteSpace(disc) ? 
                null : 
                new ReleaseNote(
                    disc,
                    NoteType.Markdown);
        }
        
        private async Task<GithubReleaseGraphQL?> GetGithubRelease(string query, string organization, string repository)
        {
            var quer = new GraphQLQuery(query, $"{{ \"org\": \"{organization}\", \"repo\": \"{repository}\" }}");
            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = new StringContent(JsonSerializer.Serialize(quer), Encoding.UTF8, "application/json")
            };
            
            using var response = await GetResponseMessage(request);
            if (response != null)
            {
                return await JsonSerializer.DeserializeAsync<GithubReleaseGraphQL>(await response.Content.ReadAsStreamAsync());
            }

            Logger.Error("Didn't get anything from Github");
            return null;
        }
    }
}