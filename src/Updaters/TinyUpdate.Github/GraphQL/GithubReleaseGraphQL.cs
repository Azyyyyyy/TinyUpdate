using System.Text.Json.Serialization;

namespace TinyUpdate.Github.GraphQL
{
    public class ReleaseAssetsNode
    {
        [JsonPropertyName("downloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class ReleaseNode
    {
        [JsonPropertyName("tagName")]
        public string TagName { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("releaseAssets")]
        public ReleaseAssets ReleaseAssets { get; set; }
    }
    
    public class ReleaseAssets
    {
        [JsonPropertyName("nodes")]
        public ReleaseAssetsNode[] Nodes { get; set; }
    }

    public class Releases
    {
        [JsonPropertyName("nodes")]
        public ReleaseNode[] Nodes { get; set; }
    }

    public class Repository
    {
        [JsonPropertyName("releases")]
        public Releases Releases { get; set; }
    }

    public class Organization
    {
        [JsonPropertyName("repository")]
        public Repository Repository { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("organization")]
        public Organization Organization { get; set; }
    }

    public class GithubReleaseGraphQL
    {
        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }
}