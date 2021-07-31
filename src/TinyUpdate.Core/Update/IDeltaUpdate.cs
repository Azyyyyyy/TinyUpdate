using System;
using System.IO;
using System.Threading.Tasks;
using TinyUpdate.Core.Temporary;

namespace TinyUpdate.Core.Update
{
    /// <summary>
    /// Handles creating and applying delta files to files
    /// </summary>
    public interface IDeltaUpdate : ISpecificOs
    {
        /// <summary>
        /// What the extension is for creates created by this <see cref="IDeltaUpdate"/>
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// Creates a delta file from two different versions of a file
        /// </summary>
        /// <param name="tempFolder">Where the temp folder is located</param>
        /// <param name="originalFileLocation">The older version of the file</param>
        /// <param name="newFileLocation">The newer version of a file</param>
        /// <param name="deltaFileLocation">Where the delta file should go if <see cref="Stream"/>s aren't supported with this <see cref="IDeltaUpdate"/></param>
        /// <param name="deltaFileStream">The delta data in a <see cref="Stream"/> if supported</param>
        /// <param name="progress">Reports back progress creating the delta file</param>
        /// <returns>If creating the delta file was successful</returns>
        public bool CreateDeltaFile(
            TemporaryFolder tempFolder,
            string originalFileLocation,
            string newFileLocation,
            string deltaFileLocation,
            out Stream? deltaFileStream,
            Action<double>? progress = null);

        /// <summary>
        /// Applies a delta file to create the newer version of a file
        /// </summary>
        /// <param name="tempFolder">Where the temp folder is located</param>
        /// <param name="originalFileLocation">The older version of the file</param>
        /// <param name="newFileLocation">Where the newer version of a file should go</param>
        /// <param name="deltaFileName">The filename of the delta file if needed</param>
        /// <param name="deltaFileStream">The delta data in a <see cref="Stream"/> if supported</param>
        /// <param name="progress">Reports back progress creating the delta file</param>
        /// <returns>If creating the delta file was successful</returns>
        public Task<bool> ApplyDeltaFile(
            TemporaryFolder tempFolder,
            string originalFileLocation,
            string newFileLocation,
            string deltaFileName,
            Stream deltaFileStream,
            Action<double>? progress = null);
    }
}