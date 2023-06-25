using System;
using SemVersion;

namespace TinyUpdate.Core;

[AttributeUsage(AttributeTargets.Assembly)]
public class SemanticVersionAttribute : Attribute
{
    //We have to pass as string or we are unable to grab it at all
    public SemanticVersionAttribute(string version)
    {
        Version = version;
    }

    public SemanticVersion Version { get; }
}