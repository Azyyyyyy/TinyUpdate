using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Build.WebApi;
using TinyUpdate.Core.Abstract;

namespace TinyUpdate.Azure;

public class AzureClient : IPackageClient
{
    private readonly IUpdateApplier _updateApplier;
    private readonly ILogger<AzureClient> _logger;
    private readonly AzureClientSettings _clientSettings;
    public AzureClient(IUpdateApplier updateApplier, ILogger<AzureClient> logger, AzureClientSettings clientSettings)
    {
        _updateApplier = updateApplier;
        _logger = logger;
        _clientSettings = clientSettings;
    }
    
    public async IAsyncEnumerable<ReleaseEntry> GetUpdates()
    {
        IAsyncEnumerable<ReleaseEntry?>? releaseEntries = null;
        using var buildClient = new BuildHttpClient(_clientSettings.OrganisationUri, _clientSettings.Credentials);
        var builds = await buildClient.GetBuildsAsync(
            _clientSettings.ProjectGuid,
            statusFilter: BuildStatus.Completed,
            resultFilter: BuildResult.Succeeded,
            top: 1);

        var build = builds.FirstOrDefault();
        if (build == null) yield break;

        var releaseArtifact = await GetArtifactAsync(build.Id, "RELEASE", buildClient);
        if (releaseArtifact == null) yield break;

        using var downloadClient = new DownloadHttpClient(_clientSettings.OrganisationUri, _clientSettings.Credentials);
        try
        {
            await using var releaseArtifactZipStream =
                await downloadClient.GetStream(releaseArtifact.Resource.DownloadUrl);
            using var releaseZip = new ZipArchive(releaseArtifactZipStream, ZipArchiveMode.Read);
            await using var releaseStream = releaseZip.GetEntry("RELEASE/RELEASE")?.Open() ?? Stream.Null;

            releaseEntries = JsonSerializer.DeserializeAsyncEnumerable<ReleaseEntry>(releaseStream);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to download RELEASE file");
        }

        if (releaseEntries != null)
        {
            await foreach (var releaseEntry in releaseEntries)
            {
                if (releaseEntry != null)
                {
                    yield return releaseEntry;
                }
            }
        }
    }

    public async Task<bool> DownloadUpdate(ReleaseEntry releaseEntry, IProgress<double>? progress)
    {
        if (releaseEntry is not AzureReleaseEntry azureReleaseEntry)
        {
            return false;
        }
        
        using var buildClient = new BuildHttpClient(_clientSettings.OrganisationUri, _clientSettings.Credentials);

        try
        {
            var dir = Path.Combine(Path.GetTempPath(), "TinyUpdate", "Azure", _clientSettings.ProjectGuid.ToString());
            Directory.CreateDirectory(dir);
            
            azureReleaseEntry.ArtifactDownloadPath = Path.Combine(dir, azureReleaseEntry.FileName);
            
            await using var artifactStream =
                await buildClient.GetArtifactContentZipAsync(_clientSettings.ProjectGuid, azureReleaseEntry.RunId, azureReleaseEntry.ArtifactName);
            await using var fileStream = File.OpenWrite(azureReleaseEntry.ArtifactDownloadPath);
            await artifactStream.CopyToAsync(fileStream);

            //TODO: Add some checks to the downloaded file?
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to download '{RunName}' artifact ({Artifact})", azureReleaseEntry.RunId, azureReleaseEntry.ArtifactName);
            return false;
        }
    }

    public Task<bool> ApplyUpdate(ReleaseEntry releaseEntry, IProgress<double>? progress)
    {
        //TODO: Load the update package in
        throw new NotImplementedException();
    }
    
    private Task<BuildArtifact?> GetArtifactAsync(int runId, string artifactName, BuildHttpClient buildClient)
    {
        try
        {
            return buildClient.GetArtifactAsync(_clientSettings.ProjectGuid, runId, artifactName);
        }
        catch (Exception e)
        { 
            _logger.LogError(e, "Failed to get pipeline artifact '{ArtifactName}'", artifactName);
        }

        return Task.FromResult<BuildArtifact?>(null);
    }
}