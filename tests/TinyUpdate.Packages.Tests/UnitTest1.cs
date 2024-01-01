using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TinyUpdate.Core;
using TinyUpdate.Core.Abstract;
using TinyUpdate.TUUP;

namespace TinyUpdate.Packages.Tests;

public class Tests
{
    protected IUpdatePackage _updatePackage;
    
    [SetUp]
    public void Setup()
    {
        var mockDeltaManager = new Mock<IDeltaManager>();
        var mockDeltaApplier1 = new Mock<IDeltaApplier>();
        var mockDeltaApplier2 = new Mock<IDeltaApplier>();

        mockDeltaApplier1.Setup(x => x.Extension).Returns(".diff");
        mockDeltaApplier2.Setup(x => x.Extension).Returns(".bsdiff");
        
        mockDeltaManager.Setup(x => x.Appliers).Returns(new[] { mockDeltaApplier1.Object, mockDeltaApplier2.Object });
        
        _updatePackage = new TuupUpdatePackage(mockDeltaManager.Object, new SHA256(NullLogger.Instance));
    }

    [Test]
    public async Task CanProcessFileData()
    {
        var fileStream = File.OpenRead(Path.Combine("Assets", _updatePackage.GetType().Name, "osu!.2023.1130.0-delta.tuup"));
        await _updatePackage.Load(fileStream);
        //TODO: Check contents is as expected
    }
}