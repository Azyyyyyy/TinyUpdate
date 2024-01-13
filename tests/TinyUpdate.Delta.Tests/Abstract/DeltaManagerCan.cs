using System.IO.Abstractions;
using Moq;
using TinyUpdate.Core;
using TinyUpdate.Core.Abstract;
using TinyUpdate.Core.Model;
using TinyUpdate.Core.Tests;
using TinyUpdate.Core.Tests.Attributes;

namespace TinyUpdate.Delta.Tests.Abstract;

public abstract class DeltaManagerCan
{
    private IFileSystem _fileSystem = null!;

    [OneTimeSetUp]
    public void BaseSetup()
    {
        _fileSystem = Functions.SetupMockFileSystem();
    }
    
    [Test]
    [DeltaApplier]
    public async Task PickSmallestDeltaFile()
    {
        var creatorList = new Mock<IDeltaCreation>[4];
        for (int i = 0; i < creatorList.Length; i++)
        {
            creatorList[i] =
                CreateMockDeltaCreation(GetRandomExtension(), true, (creatorList.Length - i) / (double)creatorList.Length);
        }
        
        var deltaManager = CreateDeltaManager(Array.Empty<IDeltaApplier>(), creatorList.Select(x => x.Object));
        await using var sourceStream = _fileSystem.File.OpenRead(Path.Combine("Assets", "original.jpg"));
        await using var targetStream = _fileSystem.File.OpenRead(Path.Combine("Assets", "new.jpg"));

        var result = await deltaManager.CreateDeltaUpdate(sourceStream, targetStream);
        Assert.Multiple(() =>
        {
            Assert.That(result.Successful, Is.True, () => "Failed to create a dummy delta file");
            Assert.That(result.Creator, Is.SameAs(creatorList[^1].Object), () => $"Incorrect {typeof(IDeltaCreation)} was selected for use");
        });
    }
    
    /*Note that if a IDeltaCreation throws an exception, we don't want to
     handle that as the IDeltaCreation should be handling them correctly!*/
    [Test]
    [DeltaApplier]
    public async Task GracefullyHandleFailedDeltaCreation()
    {
        var creatorList = new Mock<IDeltaCreation>[2];
        creatorList[0] = CreateMockDeltaCreation(GetRandomExtension(), true, 0.3);
        creatorList[1] = CreateMockDeltaCreation(GetRandomExtension(), false, 0.2);

        var deltaManager = CreateDeltaManager(Array.Empty<IDeltaApplier>(), creatorList.Select(x => x.Object));
        await using var sourceStream = _fileSystem.File.OpenRead(Path.Combine("Assets", "original.jpg"));
        await using var targetStream = _fileSystem.File.OpenRead(Path.Combine("Assets", "new.jpg"));

        var result = await deltaManager.CreateDeltaUpdate(sourceStream, targetStream);
        Assert.Multiple(() =>
        {
            Assert.That(result.Successful, Is.True, () => "Failed to create a dummy delta file");
            Assert.That(result.Creator, Is.SameAs(creatorList[0].Object), () => $"Incorrect {typeof(IDeltaCreation)} was selected for use");
        });
    }

    [Test]
    [DeltaApplier]
    public async Task ProvideFailedDeltaCreationResultOnAllFailed()
    {
        var creatorList = new Mock<IDeltaCreation>[2];
        creatorList[0] = CreateMockDeltaCreation(GetRandomExtension(), false, 0.3);
        creatorList[1] = CreateMockDeltaCreation(GetRandomExtension(), false, 0.2);

        var deltaManager = CreateDeltaManager(Array.Empty<IDeltaApplier>(), creatorList.Select(x => x.Object));
        await using var sourceStream = _fileSystem.File.OpenRead(Path.Combine("Assets", "original.jpg"));
        await using var targetStream = _fileSystem.File.OpenRead(Path.Combine("Assets", "new.jpg"));

        var result = await deltaManager.CreateDeltaUpdate(sourceStream, targetStream);
        Assert.That(result, Is.EqualTo(DeltaCreationResult.Failed), () => $"Didn't pass failed {nameof(DeltaCreationResult)}");
    }
    
    [Test]
    [DeltaApplier]
    public async Task RunAllDeltaCreatorsOnlyOnce()
    {
        var creatorList = CreateMockDeltaCreators();
        var deltaManager = CreateDeltaManager(Array.Empty<IDeltaApplier>(), creatorList.Select(x => x.Object));
        await using var sourceStream = _fileSystem.File.OpenRead(Path.Combine("Assets", "original.jpg"));
        await using var targetStream = _fileSystem.File.OpenRead(Path.Combine("Assets", "new.jpg"));

        var result = await deltaManager.CreateDeltaUpdate(sourceStream, targetStream);
        Assert.Multiple(() =>
        {
            //One will come back as successful, we want to ensure that's the case here
            Assert.That(result.Successful, Is.True, () => "Failed to create a dummy delta file");
            foreach (var mockCreator in creatorList)
            {
                Assert.DoesNotThrow(() =>
                    {
                        mockCreator.Verify(x =>
                            x.CreateDeltaFile(It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<Stream>(),
                                It.IsAny<IProgress<double>>()), Times.Once);
                    }, $"mockCreator with extension '{mockCreator.Object.Extension}' failed!");
            }
        });
    }

    protected abstract IDeltaManager CreateDeltaManager(IEnumerable<IDeltaApplier> appliers,
        IEnumerable<IDeltaCreation> creators);

    private static string GetRandomExtension() => $".{Path.GetRandomFileName()}";
    
    private Mock<IDeltaCreation>[] CreateMockDeltaCreators()
    {
        var shouldPass = true;
        var applierList = Enumerable.Range(0, 10)
            .Select(x => CreateMockDeltaCreation(GetRandomExtension(), shouldPass = !shouldPass)).ToArray();

        return applierList;
    }
    
    private static Mock<IDeltaCreation> CreateMockDeltaCreation(string extension, bool pass, double? targetFilesizePercent = null)
    {
        var mockCreation = new Mock<IDeltaCreation>();

        mockCreation.Setup(x => x.Extension).Returns(extension);
        mockCreation.Setup(x => 
                x.CreateDeltaFile(It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<IProgress<double>>()))
            .Callback((Stream sourceStream, Stream targetStream, Stream deltaStream,
                IProgress<double>? progress) =>
            {
                targetFilesizePercent ??= (long)Random.Shared.NextDouble();

                var filesize = (long)(targetStream.Length * targetFilesizePercent.Value);
                Functions.FillStreamWithRandomData(deltaStream, filesize);
            }).ReturnsAsync(pass);
        
        return mockCreation;
    }
}