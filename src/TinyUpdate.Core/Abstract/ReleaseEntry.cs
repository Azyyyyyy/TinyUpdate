using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using SemVersion;
using TinyUpdate.Core.Converters;

namespace TinyUpdate.Core.Abstract;

/// <summary>
/// Provides data about a release
/// </summary>
public abstract class ReleaseEntry
{
    protected ReleaseEntry(SemanticVersion? previousVersion, SemanticVersion newVersion, bool isDelta)
    {
        PreviousVersion = previousVersion;
        NewVersion = newVersion;
        IsDelta = isDelta;
    }

    /// <summary>
    /// If this entry contains an update to be applied
    /// </summary>
    public abstract bool HasUpdate { get; }
    
    /// <summary>
    /// What version this update package was created against
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(SemanticVersionConverter))]
    public SemanticVersion? PreviousVersion { get; }

    /// <summary>
    /// If this is a delta release
    /// </summary>
    [MemberNotNullWhen(true, nameof(PreviousVersion))]
    public bool IsDelta { get; }
    
    /// <summary>
    /// What version this update package will bump the application too
    /// </summary>
    [JsonConverter(typeof(SemanticVersionConverter))]
    public SemanticVersion NewVersion { get; }
}