using System;
using System.IO;
using System.Text.RegularExpressions;

namespace TinyUpdate.Core.Utils
{
    //TODO: Maybe move to Binary as will be different based on what was used to create the update
    /// <summary>
    /// Allows us to easily use <see cref="Version"/>'s for finding where a version of the application would be located
    /// </summary>
    public static class VersionUtil
    {
        private static readonly Regex SuffixRegex = new Regex(@"(-full|-delta)?", RegexOptions.Compiled);
        private static readonly Regex VersionRegex = new Regex(@"\d+(\.\d+){0,3}(-[A-Za-z][0-9A-Za-z-]*)?$", RegexOptions.Compiled);

        /// <summary>
        /// Gets where an certain version of the application would be located
        /// </summary>
        /// <param name="version">the <see cref="Version"/> we want the path for</param>
        /// <returns>What the <see cref="version"/> would be</returns>
        public static string GetApplicationPath(this Version version) =>
            Path.Combine(Global.ApplicationFolder, $"app-{version}");

        /// <summary>
        /// Gets the version from the filename if it exists 
        /// </summary>
        /// <param name="fileName"></param>
        public static Version? ToVersion(this string fileName)
        {
            var name = SuffixRegex.Replace(fileName, "");
            var version = VersionRegex.Match(name);
            return version.Success ? new Version(version.Value) : null;
        }
    }
}