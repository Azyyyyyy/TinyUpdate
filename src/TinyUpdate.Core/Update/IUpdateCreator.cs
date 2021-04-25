using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TinyUpdate.Core.Update
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
        /// <param name="deltaUpdateLocation">Where the delta file should be put (defaults to Temp folder with random name)</param>
        /// <param name="concurrentDeltaCreation">How many delta files we can create at the same time (NOTE: You WILL need a powerful CPU and a lot of RAM to use this)</param>
        /// <param name="intendedOS">OS that this update is intended for</param>
        /// <param name="progress">Reports back the progress of creating the update file</param>
        /// <returns>If we was able to create the package</returns>
        public Task<bool> CreateDeltaPackage(string newVersionLocation, string baseVersionLocation,
            string? deltaUpdateLocation = null, int concurrentDeltaCreation = 1, OSPlatform? intendedOS = null,
            Action<decimal>? progress = null);

        /// <summary>
        /// Creates a full package from the application folder
        /// </summary>
        /// <param name="applicationLocation">Where the application that needs to be made into a package is located</param>
        /// <param name="fullUpdateLocation">Where the update file should be put (defaults to Temp folder with random name)</param>
        /// <param name="progress">Reports back the progress of creating the update file</param>
        /// <returns>If we was able to create the package</returns>
        public Task<bool> CreateFullPackage(string applicationLocation, string? fullUpdateLocation = null,
            Action<decimal>? progress = null);

        /// <summary>
        /// Extension to be used on this kind of update file
        /// </summary>
        public string Extension { get; }
    }
}