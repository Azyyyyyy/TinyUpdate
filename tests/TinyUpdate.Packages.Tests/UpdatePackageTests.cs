using Microsoft.Extensions.Logging.Abstractions;
using TinyUpdate.Core;
using TinyUpdate.Core.Abstract;
using TinyUpdate.TUUP;

namespace TinyUpdate.Packages.Tests;

public class TuupDeltaManagerTests : UpdatePackageCan
{
    [SetUp]
    public void Setup()
    {
        var msDelta = new MSDelta.MSDelta();
        var bsDelta = new BSDelta.BSDelta(NullLogger.Instance);
        var deltaManager = new TuupDeltaManager(new IDeltaApplier[]{ msDelta, bsDelta }, new IDeltaCreation[]{ msDelta, bsDelta });

        var sha256 = new SHA256(NullLogger.Instance);
        UpdatePackage = new TuupUpdatePackage(deltaManager, sha256);
        UpdatePackageCreator = new TuupUpdatePackageCreator(sha256, deltaManager, new TuupUpdatePackageCreatorOptions());
    }
}