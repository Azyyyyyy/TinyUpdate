using Moq;
using TinyUpdate.Core.Abstract.Delta;
using TinyUpdate.Core.Services;

namespace TinyUpdate.Core.Tests;

public static class DeltaMocker
{
    public static IDeltaManager CreateDeltaManager(bool needFixedCreatorSize, Func<string, Mock<IDeltaApplier>>? applierCreator = null)
    {
        var mockApplier1 = applierCreator?.Invoke(".bsdiff") ?? CreateMockDeltaApplier(".bsdiff");
        var mockApplier2 = applierCreator?.Invoke(".diffing") ?? CreateMockDeltaApplier(".diffing");

        var mockCreation1 = CreateMockDeltaCreation(".bsdiff", needFixedCreatorSize ? 0.5 : null);
        var mockCreation2 = CreateMockDeltaCreation(".diffing", needFixedCreatorSize ? 0.3 : null);
        
        return new DeltaManager(
            [mockApplier1.Object, mockApplier2.Object],
            [mockCreation1.Object, mockCreation2.Object],
            NUnitLogger<DeltaManager>.Instance);
    }
    
    public static Mock<IDeltaApplier> CreateMockDeltaApplier(string extension)
    {
        var mockApplier = new Mock<IDeltaApplier>();
        
        mockApplier.Setup(x => x.Extension).Returns(extension);
        mockApplier.Setup(x => x.SupportedStream(It.IsAny<Stream>())).Returns(true);
        mockApplier.Setup(x => 
                x.ApplyDeltaFile(It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<IProgress<double>>()))
            .Callback((Stream sourceStream, Stream deltaStream, Stream targetStream,
                IProgress<double>? progress) => Functions.FillStreamWithRandomData(deltaStream))
            .ReturnsAsync(true);

        return mockApplier;
    }

    public static Mock<IDeltaCreation> CreateMockDeltaCreation(string extension, double? filesizePercent = null)
    {
        var mockCreation = new Mock<IDeltaCreation>();

        mockCreation.Setup(x => x.Extension).Returns(extension);
        mockCreation.Setup(x => 
                x.CreateDeltaFile(It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<IProgress<double>>()))
            .Callback((Stream sourceStream, Stream targetStream, Stream deltaStream,
                IProgress<double>? progress) =>
            {
                filesizePercent ??= Random.Shared.NextDouble();
                var filesize = (long)(targetStream.Length * filesizePercent);

                Functions.FillStreamWithRandomData(deltaStream, filesize);
            }).ReturnsAsync(true);
        
        return mockCreation;
    }

}