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
        public static readonly Regex OsRegex = new Regex("(-Linux|-Windows|-OSX)?", RegexOptions.Compiled);

        private static readonly Regex SuffixRegex = new Regex("(-full|-delta)?", RegexOptions.Compiled);
        private static readonly Regex VersionRegex =
            new Regex(@"\d+(\.\d+){0,3}(-[A-Za-z][0-9A-Za-z-]*)?$", RegexOptions.Compiled);
        
        /// <summary>
        /// Creates a <see cref="SemanticVersion"/> from <see cref="Version"/>
        /// </summary>
        /// <param name="version">Version to turn into <see cref="SemanticVersion"/></param>
        public static SemanticVersion ToSemanticVersion(this Version version)
        {
            return SemanticVersion.Parse(version.ToString(3) + GetRevisionString(version));
        }

        private static string GetRevisionString(Version version)
        {
            var s = string.Empty;
            if (version.Revision > 0)
            {
                s += "+r" + version.Revision;
            }

            return s;
        }

        /// <summary>
        /// Grabs the <see cref="SemanticVersion"/> from an assembly
        /// </summary>
        /// <param name="assembly">Assembly to look through</param>
        [return: NotNullIfNotNull("assembly")]
        public static SemanticVersion? GetSemanticVersion(this Assembly? assembly)
        {
            if (assembly == null)
            {
                return null;
            }
            
            var attributes = assembly.CustomAttributes;
            var version = attributes.Select(GetVersionFromAttribute).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            
            return version != null ?
                SemanticVersion.Parse(version) : assembly.GetName().Version.ToSemanticVersion();
        }

        private static string? GetVersionFromAttribute(CustomAttributeData attribute)
        {
            return attribute.AttributeType.FullName == typeof(SemanticVersionAttribute).FullName ? 
                attribute.ConstructorArguments[0].Value.ToString() : null;
        }

        /// <summary>
        /// Gets the version from the filename if it exists 
        /// </summary>
        /// <param name="fileName">The filename that contains a <see cref="Version"/></param>
        public static SemanticVersion? ToVersion(this string fileName)
        {
            var name = SuffixRegex.Replace(fileName, string.Empty);
            name = OsRegex.Replace(name, string.Empty);
            name = name[(name.IndexOf('-') + 1)..];

            var versionStr = VersionRegex.Match(name);
            return versionStr.Success && SemanticVersion.TryParse(versionStr.Value, out var version) 
                ? version : null;
        }
    }
}