using System;
using System.Linq;
using System.Threading.Tasks;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Core.Update
{
    /// <summary>
    /// This is used for finding any updates for this application, downloading the updates and passing the updates to be applied by an <see cref="IUpdateApplier"/>
    /// </summary>
    public abstract class UpdateChecker
    {
        protected readonly ILogging Logger;
        private readonly IUpdateApplier _updateApplier;
        protected UpdateChecker(IUpdateApplier updateApplier)
        {
            _updateApplier = updateApplier;
            Logger = LoggingCreator.CreateLogger(GetType().Name);
        }

        /// <summary>
        /// Finds any updates that are haven't been applied to the application
        /// </summary>
        /// <returns><see cref="UpdateInfo"/> with all the updates that are pending</returns>
        public abstract Task<UpdateInfo?> CheckForUpdate();

        /// <summary>
        /// Gets the changelog for a <see cref="ReleaseEntry"/> that needs to be applied
        /// </summary>
        /// <param name="entry"><see cref="ReleaseEntry"/> to grab a <see cref="ReleaseNote"/> for</param>
        /// <returns>The <see cref="ReleaseNote"/> with all the details of this <see cref="ReleaseEntry"/></returns>
        public abstract Task<ReleaseNote?> GetChangelog(ReleaseEntry entry);

        /// <summary>
        /// Grabs the newest changelog for this application
        /// </summary>
        /// <param name="updateInfo">Updates that we need to apply</param>
        /// <returns>The <see cref="ReleaseNote"/> with all the details of the newest <see cref="ReleaseEntry"/> (or null if we don't have an update to apply)</returns>
        public virtual async Task<ReleaseNote?> GetChangelog(UpdateInfo updateInfo)
        {
            if (updateInfo.HasUpdate)
            {
                return await GetChangelog(updateInfo.Updates.First(x => x.Version == updateInfo.NewVersion));
            }

            return null;
        }

        /// <summary>
        /// Downloads an update that is going to be applied
        /// </summary>
        /// <param name="releaseEntry">Update to download</param>
        /// <param name="progress">Progress of downloading update</param>
        /// <returns>If we was able to download the update</returns>
        public abstract Task<bool> DownloadUpdate(ReleaseEntry releaseEntry, Action<decimal>? progress);

        /// <summary>
        /// Downloads all updates from a <see cref="UpdateInfo"/> that are going to be applied
        /// </summary>
        /// <param name="updateInfo">Updates to download</param>
        /// <param name="progress">Progress of downloading updates</param>
        /// <returns>If we was able to download the updates</returns>
        public virtual async Task<bool> DownloadUpdate(UpdateInfo updateInfo, Action<decimal>? progress)
        {
            var updates = updateInfo.Updates.OrderBy(x => x.Version).ThenByDescending(x => x.IsDelta).ToArray();

            /*Go through every update we have, reporting the
             progress by how many updates we have*/
            var totalBytesToDownload = updates.Select(x => x.Filesize).Sum();
            var totalBytesDownloaded = 0L;
            foreach (var updateEntry in updates)
            {
                /*Base path changes based on if the first update has been done*/
                if (!await DownloadUpdate(updateEntry,
                    updateProgress =>
                    {
                        progress?.Invoke(((updateEntry.Filesize * updateProgress) + totalBytesDownloaded) /
                                         totalBytesToDownload);
                    }))
                {
                    Logger.Error("Downloading version {0} failed", updateEntry.Version);
                    return false;
                }

                totalBytesDownloaded += updateEntry.Filesize;
            }
            
            return true;
        }

        /// <inheritdoc cref="IUpdateApplier.ApplyUpdate(ReleaseEntry, Action{decimal})"/>
        public async Task<bool> ApplyUpdate(ReleaseEntry releaseEntry, Action<decimal>? progress) =>
            await _updateApplier.ApplyUpdate(releaseEntry, progress);

        /// <inheritdoc cref="IUpdateApplier.ApplyUpdate(UpdateInfo, Action{decimal})"/>
        public Task<bool> ApplyUpdate(UpdateInfo updateInfo, Action<decimal>? progress) =>
            _updateApplier.ApplyUpdate(updateInfo, progress);
    }
}