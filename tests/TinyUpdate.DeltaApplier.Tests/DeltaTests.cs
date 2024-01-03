using Microsoft.Extensions.Logging.Abstractions;
using TinyUpdate.DeltaApplier.Tests.Abstract;
using TinyUpdate.DeltaApplier.Tests.Attributes;

namespace TinyUpdate.DeltaApplier.Tests;

public class BSDeltaTests : DeltaCan
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var delta = new BSDelta.BSDelta(NullLogger.Instance);
        Creator = delta;
        Applier = delta;
    }
}

[NonWindowsIgnore]
public class MSDeltaTests : DeltaCan
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var delta = new MSDelta.MSDelta();
        Creator = delta;
        Applier = delta;
    }
}