using SemVersion;

namespace TinyUpdate.Packages.Tests.Model;

public class DeltaUpdatePackageTestData
{
    public required string Name { get; init; }
    public required string SourceFolder { get; init; }
    public required string TargetFolder { get; init; }
    public required string ApplicationName { get; init; }
    public required SemanticVersion NewVersion { get; init; }
    public required string ExpectedFilename { get; init; }
    public bool NeedsFixedCreatorSize { get; init; }
    public override string ToString() => Name;
}