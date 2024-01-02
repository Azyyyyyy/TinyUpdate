using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SemVersion;
using TinyUpdate.Core;
using TinyUpdate.Core.Abstract;
using TinyUpdate.TUUP;

namespace TinyUpdate.Packages.Tests;

public class Tests
{
    protected IUpdatePackage _updatePackage;
    protected IUpdatePackageCreation _updatePackageCreation;
    
    [SetUp]
    public void Setup()
    {
        var mockDeltaManager = new Mock<IDeltaManager>();
        var mockDeltaApplier1 = new Mock<IDeltaApplier>();
        var mockDeltaApplier2 = new Mock<IDeltaApplier>();

        mockDeltaApplier1.Setup(x => x.Extension).Returns(".diff");
        mockDeltaApplier2.Setup(x => x.Extension).Returns(".bsdiff");
        
        mockDeltaManager.Setup(x => x.Appliers).Returns(new[] { mockDeltaApplier1.Object, mockDeltaApplier2.Object });

        var sha = new SHA256(NullLogger.Instance);
        _updatePackage = new TuupUpdatePackage(mockDeltaManager.Object, sha);
        _updatePackageCreation = new TuupUpdatePackageCreation(sha);
    }

    [Test]
    public async Task CanProcessFileData()
    {
        var fileStream = File.OpenRead(Path.Combine("Assets", _updatePackage.GetType().Name, "osu!.2023.1130.0-delta.tuup"));
        await _updatePackage.Load(fileStream);
        //TODO: Check contents is as expected
    }
    
    [Test]
    public async Task CanMakeFullFile()
    {
        var location = "C:\\Users\\apearson\\source\\repos\\TinyUpdate\\src\\DeltaAppliers";
        var packageLocation = "C:\\Users\\apearson\\source\\repos\\TinyUpdate\\src";
        var version = SemanticVersion.BaseVersion();
        
        await _updatePackageCreation.CreateFullPackage(location, version, packageLocation, "testing");
        //TODO: Check contents is as expected
    }
}