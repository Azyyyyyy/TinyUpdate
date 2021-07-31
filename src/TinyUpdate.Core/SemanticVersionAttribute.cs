using System;
using SemVersion;

namespace TinyUpdate.Core
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class SemanticVersionAttribute : Attribute
    {
        public SemanticVersionAttribute(string version)
        {
            Version = version;
        }

        public SemanticVersion Version { get; }
    }
}