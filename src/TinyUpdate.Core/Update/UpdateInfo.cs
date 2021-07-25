using System;
using System.Linq;

namespace TinyUpdate.Core.Update
{
    /// <summary>
    /// Contains all the updates that we need to apply and version details
    /// </summary>
    public class UpdateInfo
    {
        public UpdateInfo(Version applicationVersion, params ReleaseEntry[] updates)
        {
            Updates = updates;

            //See if there is any update that is newer then the current version
            HasUpdate = Updates.Any(x => x.Version > applicationVersion);

            //Get the newest version if we have an update to apply
            if (HasUpdate)
            {
                NewVersion = Updates.OrderByDescending(x => x.Version).FirstOrDefault()?.Version;
            }
        }

        /// <summary>
        /// What <see cref="Version"/> this update will bump the application too
        /// </summary>
        public Version? NewVersion { get; }

        /// <summary>
        /// All the updates that we have found
        /// </summary>
        public ReleaseEntry[] Updates { get; }

        /// <summary>
        /// If we have any updates to apply
        /// </summary>
        public bool HasUpdate { get; }
    }
}