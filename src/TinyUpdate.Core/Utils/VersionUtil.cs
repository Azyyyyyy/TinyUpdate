using System;
using System.IO;

namespace TinyUpdate.Core.Utils
{
    //TODO: Maybe move to Binary as will be different based on what was used to create the update
    /// <summary>
    /// Allows us to easily use <see cref="Version"/>'s for finding where a version of the application would be located
    /// </summary>
    public static class VersionUtil
    {
        /// <summary>
        /// Gets where an certain version of the application would be located
        /// </summary>
        /// <param name="version">the <see cref="Version"/> we want the path for</param>
        /// <returns>What the <see cref="version"/> would be</returns>
        public static string GetApplicationPath(this Version version) =>
            Path.Combine(Global.ApplicationFolder, $"app-{version}");
    }
}