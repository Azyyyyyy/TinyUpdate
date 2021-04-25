using System;
using System.IO;
using System.Threading.Tasks;

namespace TinyUpdate.Core.Update
{
    /// <summary>
    /// Handles creating and applying delta files to files
    /// </summary>
    public interface IDeltaUpdate : ISpecificOS
    {
        /// <summary>
        /// What the extension is for creates created by this <see cref="IDeltaUpdate"/>
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// Creates a delta file from two different versions of a file
        /// </summary>
        /// <param name="baseFileLocation">The older version of the file</param>
        /// <param name="newFileLocation">The newer version of a file</param>
        /// <param name="deltaFileLocation">Where the delta file should go if <see cref="Stream"/>s aren't supported</param>
        /// <param name="deltaFileStream">The delta data in a <see cref="Stream"/> if supported</param>
        /// <param name="progress">Reports back progress creating the delta file</param>
        /// <returns>If creating the delta file was successful</returns>
        public bool CreateDeltaFile(
            string baseFileLocation,
            string newFileLocation,
            string deltaFileLocation,
            out Stream? deltaFileStream,
            Action<decimal>? progress = null);

        /// <summary>
        /// Applies a delta file to create the newer version of a file
        /// </summary>
        /// <param name="originalFile">The older version of the file</param>
        /// <param name="newFile">Where the newer version of a file should go</param>
        /// <param name="deltaFile">Where the delta file is if <see cref="Stream"/>s aren't supported</param>
        /// <param name="deltaStream">The delta data in a <see cref="Stream"/> if supported</param>
        /// <param name="progress">Reports back progress creating the delta file</param>
        /// <returns>If creating the delta file was successful</returns>
        public Task<bool> ApplyDeltaFile(
            string originalFile,
            string newFile,
            string deltaFile,
            Stream? deltaStream,
            Action<decimal>? progress = null);
    }
}