using Microsoft.Extensions.Logging.Abstractions;
using TinyUpdate.Core;
using TinyUpdate.Packages.Tests.Abstract;
using TinyUpdate.TUUP;

namespace TinyUpdate.Packages.Tests;

public class TuupUpdatePackageTests : UpdatePackageCan
{
    [SetUp]
    public void Setup()
    {
        var mockApplier1 = CreateMockDeltaApplier(".bsdiff");
        var mockApplier2 = CreateMockDeltaApplier(".diffing");

        var mockCreation1 = CreateMockDeltaCreation(".bsdiff");
        var mockCreation2 = CreateMockDeltaCreation(".diffing");
        
        var deltaManager = new TuupDeltaManager(
            [ mockApplier1.Object, mockApplier2.Object ],
            [ mockCreation1.Object, mockCreation2.Object ]);

        var sha256 = new SHA256(NullLogger.Instance);
        UpdatePackage = new TuupUpdatePackage(deltaManager, sha256);
        UpdatePackageCreator = new TuupUpdatePackageCreator(sha256, deltaManager, FileSystem, new TuupUpdatePackageCreatorOptions());
    }
}