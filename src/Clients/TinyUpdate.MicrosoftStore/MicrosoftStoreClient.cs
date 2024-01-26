using Windows.Services.Store;
using Microsoft.Extensions.Logging;
using TinyUpdate.Core.Abstract;

namespace TinyUpdate.MicrosoftStore;

public class MicrosoftStoreClient(StoreContext storeContext, ILogger<MicrosoftStoreClient> logger) : IPackageClient
{
    public async IAsyncEnumerable<ReleaseEntry> GetUpdates()
    {
        var storePackageUpdates = await storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
        yield return new MicrosoftStoreReleaseEntry(storePackageUpdates);
    }

    public async Task<bool> DownloadUpdate(ReleaseEntry releaseEntry, IProgress<double>? progress)
    {
        if (releaseEntry is not MicrosoftStoreReleaseEntry microsoftStoreReleaseEntry)
        {
            logger.LogError("Wasn't given a microsoft store update entry");
            return false;
        }

        var downloadOperation = storeContext.RequestDownloadStorePackageUpdatesAsync(microsoftStoreReleaseEntry
            .StorePackageUpdates);

        downloadOperation.Progress = (_, progressStatus) => progress?.Report(progressStatus.PackageDownloadProgress);
        
        var downloadResult = await downloadOperation.AsTask();
        return downloadResult.OverallState == StorePackageUpdateState.Completed;
    }

    public async Task<bool> ApplyUpdate(ReleaseEntry releaseEntry, IProgress<double>? progress)
    {
        //No need to actually do an update, report as successful
        if (!releaseEntry.HasUpdate)
        {
            progress?.Report(1);
            return true;
        }
        
        if (releaseEntry is not MicrosoftStoreReleaseEntry microsoftStoreReleaseEntry)
        {
            logger.LogError("Wasn't given a microsoft store update entry");
            return false;
        }

        //TODO: Find out if the 0.8 - 1 applies here?
        var installOperation = storeContext.RequestDownloadAndInstallStorePackageUpdatesAsync(microsoftStoreReleaseEntry
            .StorePackageUpdates);
        
        installOperation.Progress = (_, progressStatus) => progress?.Report(progressStatus.TotalDownloadProgress);
        var installResult = await installOperation.AsTask();
        return installResult.OverallState == StorePackageUpdateState.Completed;
    }
}

public class MicrosoftStoreReleaseEntry : ReleaseEntry
{
    public MicrosoftStoreReleaseEntry(IReadOnlyList<StorePackageUpdate>? storePackageUpdates)
    {
        StorePackageUpdates = storePackageUpdates ?? ArraySegment<StorePackageUpdate>.Empty;
    }

    public IReadOnlyList<StorePackageUpdate> StorePackageUpdates { get; }
    public override bool HasUpdate => StorePackageUpdates.Count > 0;
}