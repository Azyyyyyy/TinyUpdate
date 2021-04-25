using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace TinyUpdate.Core.Update
{
    /// <summary>
    /// Used for applying updates to the application
    /// </summary>
    public interface IUpdateApplier
    {
        /// <summary>
        /// Applies one update to the application 
        /// </summary>
        /// <param name="entry">Update to apply</param>
        /// <param name="progress">The progress of this update</param>
        /// <returns>If this update was successfully applied</returns>
        Task<bool> ApplyUpdate(ReleaseEntry entry, Action<decimal>? progress = null);

        /// <summary>
        /// Applies a bunch of updates to the application 
        /// </summary>
        /// <param name="updateInfo">Updates to apply</param>
        /// <param name="progress">The progress of this update</param>
        /// <returns>If this update was successfully applied</returns>
        Task<bool> ApplyUpdate(UpdateInfo updateInfo, Action<decimal>? progress = null);

        /// <summary>
        /// Gets where an certain version of the application would be located
        /// </summary>
        /// <param name="version">the <see cref="Version"/> we want the path for</param>
        /// <returns>What the <see cref="version"/> would be</returns>
        [return: NotNullIfNotNull("version")]
        string? GetApplicationPath(Version? version);

        /// <summary>
        /// Extension to be used on this kind of update file
        /// </summary>
        public string Extension { get; }
    }
}