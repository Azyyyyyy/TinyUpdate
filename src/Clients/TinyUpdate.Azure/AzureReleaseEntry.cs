using System.Text.Json.Serialization;
using SemVersion;
using TinyUpdate.Core.Abstract;

namespace TinyUpdate.Azure;

public class AzureReleaseEntry : ReleaseEntry
{
    public AzureReleaseEntry(string hash, long filesize, SemanticVersion? previousVersion, SemanticVersion newVersion, string fileName, bool isDelta, int runId, string artifactName)
    {
        Hash = hash;
        Filesize = filesize;
        PreviousVersion = previousVersion;
        NewVersion = newVersion;
        FileName = fileName;
        IsDelta = isDelta;
        RunId = runId;
        ArtifactName = artifactName;
    }

    [JsonIgnore]
    public override bool HasUpdate => true;
    
    public string Hash { get; }

    public long Filesize { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), 
     JsonConverter(typeof(SemanticVersionConverter))]
    public SemanticVersion? PreviousVersion { get; }

    [JsonConverter(typeof(SemanticVersionConverter))]
    public SemanticVersion NewVersion { get; }

    public string FileName { get; }

    public bool IsDelta { get; }

    public int RunId { get; }

    public string ArtifactName { get; }

    [JsonIgnore]
    public string ArtifactDownloadPath { get; set; }
}