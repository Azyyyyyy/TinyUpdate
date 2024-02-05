using SemVersion;
using TinyUpdate.Core.Abstract;

namespace TinyUpdate.Appliers.Tests.Models;

public class MockReleaseEntry(SemanticVersion? previousVersion, SemanticVersion newVersion, bool isDelta)
    : ReleaseEntry(previousVersion, newVersion, isDelta)
{
    public override bool HasUpdate => true;
}