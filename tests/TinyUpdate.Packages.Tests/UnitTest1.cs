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
        var mockDeltaApplier1 = new MSDelta.MSDelta();
        var mockDeltaApplier2 = new BSDelta.BSDelta(NullLogger.Instance);
        var mockDeltaManager = new TuupDeltaManager(new IDeltaApplier[]{ mockDeltaApplier1, mockDeltaApplier2 }, new IDeltaCreation[]{ mockDeltaApplier1, mockDeltaApplier2 });

        var sha = new SHA256(NullLogger.Instance);
        _updatePackage = new TuupUpdatePackage(mockDeltaManager, sha);
        _updatePackageCreation = new TuupUpdatePackageCreation(sha, mockDeltaManager);
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
    
    [Test]
    public async Task CanMakeDeltaFile()
    {
        var oldLocation = "E:\\Other Games\\osu! versions (Public)\\osu!lazer\\14 2018 Apr";
        var newLocation = "E:\\Other Games\\osu! versions (Public)\\osu!lazer\\16 2018 Jun";
        var packageLocation = "E:\\Other Games\\osu! versions (Public)\\osu!lazer";
        var oldVersion = SemanticVersion.BaseVersion();
        var newVersion = SemanticVersion.BaseVersion();
        
        await _updatePackageCreation.CreateDeltaPackage(oldLocation, oldVersion, newLocation, newVersion, packageLocation, "testing");
        //TODO: Check contents is as expected
    }
}