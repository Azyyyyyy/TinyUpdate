using System.IO.Abstractions;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Build.WebApi;
using SemVersion;
using TinyUpdate.Core;
using TinyUpdate.Core.Abstract;
using TinyUpdate.Core.Abstract.UpdatePackage;

namespace TinyUpdate.Azure;

public class AzureClient : IPackageClient
{
    private readonly IUpdateApplier _updateApplier;
    private readonly IUpdatePackageFactory _updatePackageFactory;
    private readonly ILogger<AzureClient> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly AzureClientSettings _clientSettings;
    
    public AzureClient(IUpdateApplier updateApplier, ILogger<AzureClient> logger, AzureClientSettings clientSettings, IUpdatePackageFactory updatePackageFactory, IFileSystem fileSystem)
    {
        _updateApplier = updateApplier;
        _logger = logger;
        _clientSettings = clientSettings;
        _updatePackageFactory = updatePackageFactory;
        _fileSystem = fileSystem;
    }
    
    public async IAsyncEnumerable<ReleaseEntry> GetUpdates()
    {
        IAsyncEnumerable<ReleaseEntry?>? releaseEntries;
        using var buildClient = new BuildHttpClient(_clientSettings.OrganisationUri, _clientSettings.Credentials);
        var builds = await buildClient.GetBuildsAsync(
            _clientSettings.ProjectGuid,
            statusFilter: BuildStatus.Completed,
            resultFilter: BuildResult.Succeeded,
            top: 1);

        var build = builds.FirstOrDefault();
        if (build == null) yield break;

        var releaseArtifact = await GetArtifact(build.Id, "RELEASE", buildClient);
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
            yield break;
        }

        var currentVersion = SemanticVersion.Parse(VersionHelper.GetVersionDetails().Version.ToString());
        await foreach (var releaseEntry in releaseEntries)
        {
            if (releaseEntry != null 
                && (releaseEntry.NewVersion > currentVersion || !releaseEntry.IsDelta))
            {
                yield return releaseEntry;
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
            _fileSystem.Directory.CreateDirectory(dir);
            
            azureReleaseEntry.ArtifactDownloadPath = Path.Combine(dir, azureReleaseEntry.FileName);
            
            await using var artifactStream =
                await buildClient.GetArtifactContentZipAsync(_clientSettings.ProjectGuid, azureReleaseEntry.RunId, azureReleaseEntry.ArtifactName);

            var fileStream = _fileSystem.File.OpenWrite(azureReleaseEntry.ArtifactDownloadPath);
            await artifactStream.CopyToAsync(fileStream);
            await fileStream.DisposeAsync();

            //TODO: Ensure that we use correct entry as this will be the zip within a zip :/
            var readFileStream = _fileSystem.File.OpenRead(azureReleaseEntry.ArtifactDownloadPath);
            
            //We want to reopen the file with read only access
            await _updatePackageFactory.CreateUpdatePackage(readFileStream,
                Path.GetExtension(azureReleaseEntry.ArtifactDownloadPath), releaseEntry);

            //TODO: Add some checks to the downloaded file?
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to download '{RunName}' artifact ({Artifact})", azureReleaseEntry.RunId, azureReleaseEntry.ArtifactName);
            return false;
        }
    }

    public Task<bool> ApplyUpdate(IUpdatePackage updatePackage, IProgress<double>? progress) => 
        _updateApplier.ApplyUpdate(updatePackage, new DirectoryInfo(Environment.CurrentDirectory).Parent.FullName, progress);

    private Task<BuildArtifact?> GetArtifact(int runId, string artifactName, BuildHttpClientBase buildClient)
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