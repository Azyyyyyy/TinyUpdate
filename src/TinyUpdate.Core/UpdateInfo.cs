using System;
using System.Collections.Generic;
using System.Linq;

namespace TinyUpdate.Core
{
    /// <summary>
    /// Contains all the updates that we need to apply and version details
    /// </summary>
    public class UpdateInfo
    {
        public UpdateInfo(IEnumerable<ReleaseEntry> updates)
        {
            Updates = updates;

            //Get the new version if it exists
            if (HasUpdate = updates.Any())
            {
                NewVersion = updates.OrderBy(x => x.Version).FirstOrDefault().Version;
            }
        }

        /// <summary>
        /// What <see cref="Version"/> this update will bump the application too
        /// </summary>
        public Version? NewVersion { get; }

        /// <summary>
        /// All the updates that we have found
        /// </summary>
        public IEnumerable<ReleaseEntry> Updates { get; }

        /// <summary>
        /// If we have any updates to apply
        /// </summary>
        public bool HasUpdate { get; }
    }
}