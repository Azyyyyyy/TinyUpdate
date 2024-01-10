using SemVersion;

namespace TinyUpdate.Packages.Tests.Model;

public class FullUpdatePackageTestData
{
    public required string Name { get; init; }
    public required string SourceFolder { get; init; }
    public required string ApplicationName { get; init; }
    public required SemanticVersion Version { get; init; }
    public required string ExpectedFilename { get; init; }
    public override string ToString() => Name;
}