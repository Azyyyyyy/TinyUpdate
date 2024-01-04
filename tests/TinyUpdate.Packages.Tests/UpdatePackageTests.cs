using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TinyUpdate.Core;
using TinyUpdate.Core.Abstract;
using TinyUpdate.Packages.Tests.Abstract;
using TinyUpdate.TUUP;

namespace TinyUpdate.Packages.Tests;

public class TuupUpdatePackageTests : UpdatePackageCan
{
    [SetUp]
    public async Task Setup()
    {
        var mockApplier1 = CreateMockDeltaApplier(".bsdiff");
        var mockApplier2 = CreateMockDeltaApplier(".diff");

        var mockCreation1 = CreateMockDeltaCreation(".bsdiff");
        var mockCreation2 = CreateMockDeltaCreation(".diff");
        
        var deltaManager = new TuupDeltaManager(
            new IDeltaApplier[]{ mockApplier1.Object, mockApplier2.Object }, 
            new IDeltaCreation[]{ mockCreation1.Object, mockCreation2.Object });

        var sha256 = new SHA256(NullLogger.Instance);
        UpdatePackage = new TuupUpdatePackage(deltaManager, sha256);
        UpdatePackageCreator = new TuupUpdatePackageCreator(sha256, deltaManager, FileSystem, new TuupUpdatePackageCreatorOptions());
    }

    Mock<IDeltaApplier> CreateMockDeltaApplier(string extension)
    {
        var mockApplier = new Mock<IDeltaApplier>();
        
        mockApplier.Setup(x => x.Extension).Returns(extension);
        mockApplier.Setup(x => x.SupportedStream(It.IsAny<Stream>())).Returns(true);
        mockApplier.Setup(x => 
            x.ApplyDeltaFile(It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<IProgress<double>>()))
            .Callback(async (Stream sourceFileStream, Stream deltaFileStream, Stream targetFileStream,
                IProgress<double>? progress) => FillStreamWithRandomData(deltaFileStream))
            .ReturnsAsync(true);

        return mockApplier;
    }
    
    
    
    Mock<IDeltaCreation> CreateMockDeltaCreation(string extension)
    {
        var mockCreation = new Mock<IDeltaCreation>();

        mockCreation.Setup(x => x.Extension).Returns(extension);
        mockCreation.Setup(x => 
                x.CreateDeltaFile(It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<IProgress<double>>()))
            .Callback((Stream sourceFileStream, Stream deltaFileStream, Stream targetFileStream,
                IProgress<double>? progress) => FillStreamWithRandomData(deltaFileStream))
            .ReturnsAsync(true);
        
        return mockCreation;
    }
}