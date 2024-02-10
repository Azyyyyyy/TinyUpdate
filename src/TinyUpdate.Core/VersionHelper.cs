using System.Reflection;
using SemVersion;
using TinyUpdate.Core.Model;

namespace TinyUpdate.Core;

public static class VersionHelper
{
    public static SemanticVersion GetSemanticVersion(this ReadOnlySpan<char> version) => SemanticVersion.Parse(GetVersionDetails(version).Version.ToString());
    public static ReadOnlySpan<char> GetVersion(this ReadOnlySpan<char> version) => GetVersionDetails(version).Version;
    public static ReadOnlySpan<char> GetSourceRevisionId(this ReadOnlySpan<char> version) => GetVersionDetails(version).SourceRevisionId;

    public static VersionDetails GetVersionDetails() => GetVersionDetails(Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly());
    public static VersionDetails GetVersionDetails(this Assembly assembly)
    {
        var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        var version = versionAttribute?.InformationalVersion;
        return string.IsNullOrWhiteSpace(version) ? new VersionDetails() : GetVersionDetails(version);
    }
    
    public static VersionDetails GetVersionDetails(this ReadOnlySpan<char> version)
    {
        var versionDetails = new VersionDetails(version);
    
        var plusIndex = version.IndexOf('+');
        if (plusIndex != -1)
        {
            var tmpSpan = version[++plusIndex..];
            var dotIndex = tmpSpan.IndexOf('.');
            var hasDot = dotIndex != -1;

            if (hasDot)
            {
                versionDetails.SourceRevisionId = tmpSpan[(dotIndex + 1)..];
                versionDetails.Version = version[..^(versionDetails.SourceRevisionId.Length + 1)];   
            }
            else if (tmpSpan.Length == 40)
            {
                versionDetails.SourceRevisionId = tmpSpan;
                versionDetails.Version = version[..^41];
            }
        }

        return versionDetails;
    }
}