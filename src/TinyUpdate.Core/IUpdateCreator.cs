using System;
using System.Threading.Tasks;

namespace TinyUpdate.Core
{
    /// <summary>
    /// Creates updates that a <see cref="IUpdateApplier"/> can apply
    /// </summary>
    public interface IUpdateCreator
    {
        /// <summary>
        /// Creates a delta package from two versions of the application
        /// </summary>
        /// <param name="newVersionLocation">The new version of the application</param>
        /// <param name="baseVersionLocation">The version that the user will be upgrading from</param>
        /// <param name="progress">Reports back the progress of creating the update file</param>
        /// <returns>If we was able to create the package</returns>
        public Task<bool> CreateDeltaPackage(string newVersionLocation, string baseVersionLocation, Action<decimal>? progress = null);

        /// <summary>
        /// Creates a full package from the application folder
        /// </summary>
        /// <param name="applicationLocation">Where the application that needs to be made into a package is located</param>
        /// <param name="progress">Reports back the progress of creating the update file</param>
        /// <returns>If we was able to create the package</returns>
        public Task<bool> CreateFullPackage(string applicationLocation, Action<decimal>? progress = null);
    }
}