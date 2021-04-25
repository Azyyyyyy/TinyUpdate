using System;
using System.Text.RegularExpressions;

namespace TinyUpdate.Core.Extensions
{
    /// <summary>
    /// Allows us to easily use <see cref="Version"/>'s for finding where a version of the application would be located
    /// </summary>
    public static class VersionExt
    {
        private static readonly Regex SuffixRegex = new Regex(@"(-full|-delta)?", RegexOptions.Compiled);

        private static readonly Regex VersionRegex =
            new Regex(@"\d+(\.\d+){0,3}(-[A-Za-z][0-9A-Za-z-]*)?$", RegexOptions.Compiled);

        /// <summary>
        /// Gets the version from the filename if it exists 
        /// </summary>
        /// <param name="fileName">The filename that contains a <see cref="Version"/></param>
        public static Version? ToVersion(this string fileName)
        {
            var name = SuffixRegex.Replace(fileName, "");
            var version = VersionRegex.Match(name);
            return version.Success ? new Version(version.Value) : null;
        }
    }
}