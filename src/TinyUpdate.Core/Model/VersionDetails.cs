namespace TinyUpdate.Core.Model;

public ref struct VersionDetails
{
    public VersionDetails(ReadOnlySpan<char> version)
    {
        Version = version;
    }

    public ReadOnlySpan<char> Version { get; internal set; } = ReadOnlySpan<char>.Empty;
    public ReadOnlySpan<char> SourceRevisionId { get; internal set; } = ReadOnlySpan<char>.Empty;
}