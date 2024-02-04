using SemVersion;
using TinyUpdate.Core.Abstract;

namespace TinyUpdate.Appliers.Tests.Models;

public class MockReleaseEntry : ReleaseEntry
{
    public MockReleaseEntry(SemanticVersion? previousVersion, SemanticVersion newVersion, bool isDelta) : base(previousVersion, newVersion, isDelta)
    { }

    public override bool HasUpdate => true;
}