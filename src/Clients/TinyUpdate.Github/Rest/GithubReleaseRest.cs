using System;
using System.Text.Json.Serialization;
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable 8618

namespace TinyUpdate.Github.Rest
{
    public class Asset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }
    }

    public class GithubReleaseRest
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }
        
        [JsonPropertyName("assets")]
        public Asset[] Assets { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }
    }
}