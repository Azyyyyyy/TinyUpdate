using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SemVersion;

namespace TinyUpdate.Core.Extensions
{
    /// <summary>
    /// Allows us to easily use <see cref="Version"/>'s for finding where a version of the application would be located
    /// </summary>
    public static class VersionExt
    {
        private static readonly Regex SuffixRegex = new Regex(@"(-full|-delta)?", RegexOptions.Compiled);
        public static readonly Regex OsRegex = new Regex(@$"(-Linux|-Windows|-OSX)?", RegexOptions.Compiled);

        private static readonly Regex VersionRegex =
            new Regex(@"\d+(\.\d+){0,3}(-[A-Za-z][0-9A-Za-z-]*)?$", RegexOptions.Compiled);

        public static SemanticVersion ToSemanticVersion(this Version version)
        {
            return SemanticVersion.Parse(version.ToString(3) + GetRevisionString(version));
        }

        private static string GetRevisionString(Version version)
        {
            var s = "";
            if (version.Revision != -1)
            {
                s += "+r" + version.Revision;
            }

            return s;
        }

        [return: NotNullIfNotNull("assembly")]
        public static SemanticVersion? GetSemanticVersion(this Assembly? assembly)
        {
            if (assembly == null)
            {
                return null;
            }
            
            var attributes = assembly.CustomAttributes;
            var attribute = attributes.FirstOrDefault(x => x.AttributeType == typeof(SemanticVersionAttribute));
            
            return attribute != null ?
                SemanticVersion.Parse(attribute.ConstructorArguments[0].Value.ToString()) : assembly.GetName().Version.ToSemanticVersion();
        }

        /// <summary>
        /// Gets the version from the filename if it exists 
        /// </summary>
        /// <param name="fileName">The filename that contains a <see cref="Version"/></param>
        public static SemanticVersion? ToVersion(this string fileName)
        {
            var name = SuffixRegex.Replace(fileName, "");
            name = OsRegex.Replace(name, "");
            name = name[(name.IndexOf('.') + 1)..];

            var versionStr = VersionRegex.Match(name);
            return versionStr.Success && SemanticVersion.TryParse(versionStr.Value, out var version) 
                ? version : null;
        }
    }
}