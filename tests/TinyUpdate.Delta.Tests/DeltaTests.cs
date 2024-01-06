using Microsoft.Extensions.Logging.Abstractions;
using TinyUpdate.Delta.Tests.Abstract;
using TinyUpdate.Tests.Common.Attributes;

namespace TinyUpdate.Delta.Tests;

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
        //We don't actually need this but makes warnings go away
        if (OperatingSystem.IsWindows())
        {
            var delta = new MSDelta.MSDelta();
            Creator = delta;
            Applier = delta;
        }
    }
}