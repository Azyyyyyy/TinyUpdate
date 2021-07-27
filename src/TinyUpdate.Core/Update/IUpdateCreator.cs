using System;
using System.Runtime.InteropServices;
using SemVersion;

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
        /// <param name="applicationMetadata">Metadata about the application we are updating</param>
        /// <param name="newVersionLocation">The new version of the application</param>
        /// <param name="newVersion">What version this application will be updated too</param>
        /// <param name="baseVersionLocation">The version that the user will be upgrading from</param>
        /// <param name="oldVersion">What version the older application is</param>
        /// <param name="deltaUpdateLocation">Where the delta file should be put (defaults to Temp folder with random name)</param>
        /// <param name="outputFolder">Where the update file is going to be</param>
        /// <param name="intendedOs">OS that this update is intended for</param>
        /// <param name="progress">Reports back the progress of creating the update file</param>
        /// <returns>If we was able to create the package</returns>
        public bool CreateDeltaPackage(
            ApplicationMetadata applicationMetadata,
            string newVersionLocation, 
            SemanticVersion newVersion,
            string baseVersionLocation,
            SemanticVersion oldVersion,
            string outputFolder,
            string? deltaUpdateLocation = null, 
            OSPlatform? intendedOs = null,
            Action<double>? progress = null);

        /// <summary>
        /// Creates a full package from the application folder
        /// </summary>
        /// <param name="applicationMetadata">Metadata about the application we are updating</param>
        /// <param name="applicationLocation">Where the application that needs to be made into a package is located</param>
        /// <param name="fullUpdateLocation">Where the update file should be put (defaults to Temp folder with random name)</param>
        /// <param name="version">What version this application currently is</param>
        /// <param name="intendedOs">OS that this update is intended for</param>
        /// <param name="progress">Reports back the progress of creating the update file</param>
        /// <returns>If we was able to create the package</returns>
        public bool CreateFullPackage(
            ApplicationMetadata applicationMetadata,
            string applicationLocation, 
            SemanticVersion version,
            string? fullUpdateLocation = null,
            OSPlatform? intendedOs = null,
            Action<double>? progress = null);

        /// <summary>
        /// Extension to be used on this kind of update file
        /// </summary>
        public string Extension { get; }
    }
}