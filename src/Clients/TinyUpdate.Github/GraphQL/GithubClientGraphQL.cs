﻿using System;
using System.Linq;
using System.Net;
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
    /// <see cref="GithubApi"/> that uses Github's GraphQL api (V4). This will require a personal token
    /// </summary>
    // ReSharper disable once InconsistentNaming
    internal class GithubApiGraphQL : GithubApi
    {
        public GithubApiGraphQL(string personalToken) 
            : base("https://api.github.com/graphql")
        {
            //Make personalToken into Base64
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

        public override async Task<UpdateInfo?> CheckForUpdate(string organization, string repository)
        {
            using var response = await GetResponseMessage(new GraphQLQuery(UpdateQuery, $"{{ \"org\": \"{organization}\", \"repo\": \"{repository}\" }}"));
            if (response == null)
            {
                return null;
            }
            
            //Check that we can make what we got into GithubReleaseGraphQL
            var releases = await JsonSerializer.DeserializeAsync<GithubReleaseGraphQL>(await response.Content.ReadAsStreamAsync());
            if (releases == null)
            {
                Logger.Error("We can't use what was returned from GitHub API");
                return null;
            }

            //Check that we got a release file from the response
            var release = releases.Data.Organization.Repository.Releases.Nodes.FirstOrDefault();
            if (release?.ReleaseAssets.Nodes.FirstOrDefault()?.Name != "RELEASES")
            {
                Logger.Error("We can't find any RELEASES file in the newest github release");
                return null;
            }
            Logger.Information("RELEASES file exists in newest github release, downloading if not already downloaded");

            return await DownloadAndParseReleaseFile(release.TagName, release.ReleaseAssets.Nodes.First().Size, release.ReleaseAssets.Nodes.First().DownloadUrl);
        }

        public override async Task<ReleaseNote?> GetChangelog(ReleaseEntry entry, string organization, string repository)
        {
            using var response = await GetResponseMessage(new GraphQLQuery(ChangeLogQuery, $"{{ \"org\": \"{organization}\", \"repo\": \"{repository}\" }}"));
            if (response == null)
            {
                return null;
            }
            
            //Check that we can make what we got into GithubReleaseGraphQL
            var releases = await JsonSerializer.DeserializeAsync<GithubReleaseGraphQL>(await response.Content.ReadAsStreamAsync());
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
        
        private async Task<HttpResponseMessage?> GetResponseMessage(GraphQLQuery query)
        {
            //TODO: Handle errors
            //TODO: Handle when we get rate limited
            //TODO: Add something to not crash when we have no wifi
            
            //Make request for getting the newest update
            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json")
            };

            //Check that we got something from it
            var response = await HttpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return response;
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