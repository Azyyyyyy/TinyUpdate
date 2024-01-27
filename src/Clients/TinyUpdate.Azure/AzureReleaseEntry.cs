using System.Text.Json.Serialization;
using SemVersion;
using TinyUpdate.Core.Abstract;

namespace TinyUpdate.Azure;

public class AzureReleaseEntry : ReleaseEntry
{
    public AzureReleaseEntry(string hash, long filesize, SemanticVersion? previousVersion, SemanticVersion newVersion, string fileName, bool isDelta, int runId, string artifactName)
        : base(previousVersion, newVersion, isDelta)
    {
        Hash = hash;
        Filesize = filesize;
        FileName = fileName;
        RunId = runId;
        ArtifactName = artifactName;
    }

    [JsonIgnore]
    public override bool HasUpdate => true;
    
    public string Hash { get; }

    public long Filesize { get; }
    
    public string FileName { get; }
    
    public int RunId { get; }

    public string ArtifactName { get; }

    [JsonIgnore]
    public string ArtifactDownloadPath { get; set; }
}