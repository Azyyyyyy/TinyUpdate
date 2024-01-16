using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using SemVersion;
using TinyUpdate.Core.Abstract;
using Artifact = Microsoft.Azure.Pipelines.WebApi.Artifact;

namespace TinyUpdate.Azure;

public class AzureClient : IPackageClient
{
    const string c_collectionUri = "";
    const string project = "";
    const int pipelineId = 1;
    private const string personalAccessToken = "";

    private readonly Uri _orgUrl = new Uri(c_collectionUri);

    private readonly HttpClient _downloadClient;
    private readonly IUpdateApplier _updateApplier;
    private readonly ILogger _logger;
    private readonly IHasher _hasher;
    public AzureClient(IUpdateApplier updateApplier, ILogger logger, HttpClient downloadClient, IHasher hasher)
    {
        _updateApplier = updateApplier;
        _logger = logger;
        _downloadClient = downloadClient;
        _hasher = hasher;
    }
    
	//TODO: REDO
    public async IAsyncEnumerable<ReleaseEntry> GetUpdates()
    {
        using var connection = MakeConnection();
        using var pipelineConnection = new PipelinesHttpClient(connection.Uri, connection.Credentials);
        
        //TODO: Find the first release which allow
        var runs = await pipelineConnection.ListRunsAsync(project, pipelineId);
        foreach (var run in runs)
        {
            if (run == null || run.State != RunState.Completed 
                            || run.Result.GetValueOrDefault(RunResult.Failed) != RunResult.Succeeded)
            {
                continue;
            }

            Artifact artifact;
            try
            {
                artifact = await pipelineConnection.GetArtifactAsync(project, pipelineId, run.Id, "RELEASE");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get RELEASE artifact");
                continue;
            }

            if (artifact.SignedContent.SignatureExpires >= DateTime.Now)
            {
                _logger.LogWarning("Unable to download RELEASE file");
                yield break;
            }

            await using var releaseFileStream = await _downloadClient.GetStreamAsync(artifact.SignedContent.Url);
            IAsyncEnumerable<AzureReleaseEntry?> releases;

            try
            {
                releases = JsonSerializer.DeserializeAsyncEnumerable<AzureReleaseEntry>(releaseFileStream);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to download RELEASE artifact");
                continue;
            }
            
            await foreach (var release in releases)
            {
                if (release != null && _hasher.IsValidHash(release.Hash))
                {
                    yield return release;
                }
            }
        }
    }

    public Task<bool> DownloadUpdate(ReleaseEntry releaseEntry, IProgress<double>? progress)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ApplyUpdate(ReleaseEntry releaseEntry, IProgress<double>? progress)
    {
        throw new NotImplementedException();
    }
    
    private VssConnection MakeConnection() => new VssConnection(_orgUrl, new VssBasicCredential(string.Empty, personalAccessToken));
}

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
}

public class SemanticVersionConverter : JsonConverter<SemanticVersion>
{
    public override SemanticVersion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return SemanticVersion.Parse(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, SemanticVersion value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}