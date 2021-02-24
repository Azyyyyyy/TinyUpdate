using System;
using System.Linq;
using System.Threading.Tasks;

namespace TinyUpdate.Core
{
    /// <summary>
    /// This is used for finding any updates for this application, downloading the updates and passing the updates to be applied by an <see cref="IUpdateApplier"/>
    /// </summary>
    public abstract class UpdateChecker
    {
        private readonly IUpdateApplier _updateApplier;
        public UpdateChecker(IUpdateApplier updateApplier)
        {
            _updateApplier = updateApplier;
        }

        /// <summary>
        /// Finds any updates that are to be applied to the application
        /// </summary>
        /// <returns><see cref="UpdateInfo"/> with all updates that are pending</returns>
        public virtual Task<UpdateInfo> CheckForUpdate() => throw new NotImplementedException();

        /// <summary>
        /// Gets the changelog for a <see cref="ReleaseEntry"/> that needs to be applied
        /// </summary>
        /// <param name="entry"><see cref="ReleaseEntry"/> to grab a <see cref="ReleaseNote"/> for</param>
        /// <returns>The <see cref="ReleaseNote"/> with all the details of this <see cref="ReleaseEntry"/></returns>
        public virtual Task<ReleaseEntry> GetChangelog(ReleaseEntry entry) => throw new NotImplementedException();

        /// <summary>
        /// Grabs the newest changelog for this application
        /// </summary>
        /// <param name="updateInfo">Updates that we need to apply</param>
        /// <returns>The <see cref="ReleaseNote"/> with all the details of the newest <see cref="ReleaseEntry"/> (or null if we don't have an update to apply)</returns>
        public virtual async Task<ReleaseEntry?> GetLatestChangelog(UpdateInfo updateInfo)
        {
            if (updateInfo.HasUpdate)
            {
                //TODO: Get newest update
                return await GetChangelog(updateInfo.Updates.Last());
            }

            return null;
        }

        /// <summary>
        /// Downloads an update that is going to be applied
        /// </summary>
        /// <param name="releaseEntry">Update to download</param>
        /// <param name="progress">Progress of downloading update</param>
        /// <returns>If we was able to download the update</returns>
        public virtual async Task<bool> DownloadUpdate(ReleaseEntry releaseEntry, Action<decimal>? progress) =>
            await _updateApplier.ApplyUpdate(releaseEntry, progress);

        /// <summary>
        /// Downloads all updates from a <see cref="UpdateInfo"/> that are going to be applied
        /// </summary>
        /// <param name="updateInfo">Updates to download</param>
        /// <param name="progress">Progress of downloading updates</param>
        /// <returns>If we was able to download the updates</returns>
        public virtual Task<bool> DownloadUpdate(UpdateInfo updateInfo, Action<decimal>? progress) =>
            _updateApplier.ApplyUpdate(updateInfo, progress);
    }
}