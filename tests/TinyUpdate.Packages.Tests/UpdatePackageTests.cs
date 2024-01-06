using Microsoft.Extensions.Logging.Abstractions;
using TinyUpdate.Core;
using TinyUpdate.Packages.Tests.Abstract;
using TinyUpdate.Packages.Tests.Attributes;
using TinyUpdate.TUUP;

namespace TinyUpdate.Packages.Tests;

public class TuupUpdatePackageTests : UpdatePackageCan
{
    [SetUp]
    public void Setup()
    {
        var mockApplier1 = CreateMockDeltaApplier(".bsdiff");
        var mockApplier2 = CreateMockDeltaApplier(".diffing");

        var mockCreation1 = CreateMockDeltaCreation(".bsdiff", NeedsFixedCreatorSize ? 0.5 : null);
        var mockCreation2 = CreateMockDeltaCreation(".diffing", NeedsFixedCreatorSize ? 0.3 : null);
        
        var deltaManager = new DeltaManager(
            [ mockApplier1.Object, mockApplier2.Object ],
            [ mockCreation1.Object, mockCreation2.Object ]);

        var sha256 = new SHA256(NullLogger.Instance);
        UpdatePackage = new TuupUpdatePackage(deltaManager, sha256);
        UpdatePackageCreator = new TuupUpdatePackageCreator(sha256, deltaManager, FileSystem, new TuupUpdatePackageCreatorOptions());
    }

    protected bool NeedsFixedCreatorSize =>
        TestContext.CurrentContext.Test.Properties.ContainsKey(FixedCreatorSizeAttribute.PropName);

    //TODO: Imp from here
    protected override void CheckNewFileInRootUpdatePackage(Stream targetFileStream, Stream expectedTargetFileStream)
    {
    }

    protected override void CheckNewFileInSubDirUpdatePackage(Stream targetFileStream, Stream expectedTargetFileStream)
    {
    }

    protected override void CheckDeltaFileInRootUpdatePackage(Stream targetFileStream, Stream expectedTargetFileStream)
    {
    }

    protected override void CheckDeltaFileInSubDirUpdatePackage(Stream targetFileStream, Stream expectedTargetFileStream)
    {
    }

    protected override void CheckMovedFileInRootUpdatePackage(Stream targetFileStream, Stream expectedTargetFileStream)
    {
    }

    protected override void CheckMovedFileRootToSubDirUpdatePackage(Stream targetFileStream, Stream expectedTargetFileStream)
    {
    }

    protected override void CheckMovedFileSubDirToRootUpdatePackage(Stream targetFileStream, Stream expectedTargetFileStream)
    {
    }

    protected override void CheckMovedFileSubDirToSubDirUpdatePackage(Stream targetFileStream, Stream expectedTargetFileStream)
    {
    }
}