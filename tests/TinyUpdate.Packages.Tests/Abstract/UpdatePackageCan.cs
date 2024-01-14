using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Text.Json;
using Moq;
using SemVersion;
using TinyUpdate.Core.Abstract;
using TinyUpdate.Core.Abstract.Delta;
using TinyUpdate.Core.Model;
using TinyUpdate.Core.Tests;
using TinyUpdate.Core.Tests.Attributes;
using TinyUpdate.Packages.Tests.Model;

namespace TinyUpdate.Packages.Tests.Abstract;

public abstract class UpdatePackageCan
{
    protected IUpdatePackage UpdatePackage = null!;
    protected IDeltaUpdatePackageCreator DeltaPackageCreator = null!;
    protected IUpdatePackageCreator FullPackageCreator = null!;
    protected IFileSystem FileSystem = null!;

    [OneTimeSetUp]
    public void BaseSetup()
    {
        FileSystem = Functions.SetupMockFileSystem();
    }
    
    [Test]
    public async Task ProcessFileData()
    {
        var baseFilePath = Path.Combine("Assets", UpdatePackage.GetType().Name);
        await using var updatePackageStream = FileSystem.File.OpenRead(Path.Combine(baseFilePath, "exampleUpdatePackage" + UpdatePackage.Extension));
        await UpdatePackage.Load(updatePackageStream);

        IReadOnlyCollection<FileEntry>? expectedDeltaFiles = null, expectedMovedFiles = null, expectedNewFiles = null, expectedUnchangedFiles = null;
        await Assert.MultipleAsync(async () =>
        {
            expectedDeltaFiles = await GetJsonFile("exampleUpdatePackage_expectedDeltas.json");
            expectedMovedFiles = await GetJsonFile("exampleUpdatePackage_expectedMoved.json");
            expectedNewFiles = await GetJsonFile("exampleUpdatePackage_expectedNew.json");
            expectedUnchangedFiles  = await GetJsonFile("exampleUpdatePackage_expectedUnchanged.json");
        });
        if (expectedMovedFiles == null || expectedDeltaFiles == null || expectedNewFiles == null || expectedUnchangedFiles == null)
        {
            Assert.Fail("Unable to test as expected JSON files contain nothing");
            return;
        }
        
        Assert.Multiple(() =>
        {
            CheckEntries(expectedDeltaFiles, UpdatePackage.DeltaFiles, true);
            CheckEntries(expectedNewFiles, UpdatePackage.NewFiles, true);
            CheckEntries(expectedMovedFiles, UpdatePackage.MovedFiles, false);
            CheckEntries(expectedUnchangedFiles, UpdatePackage.UnchangedFiles, false);
        });
        return;


        async Task<IReadOnlyCollection<FileEntry>?> GetJsonFile(string filename)
        {
            var fileLocation = Path.Combine(baseFilePath, filename);
            if (!FileSystem.File.Exists(fileLocation))
            {
                Assert.Warn($"We don't have '{filename}' for testing {UpdatePackage.GetType().Name} FileData loading");
                return null;
            }

            return JsonSerializer.Deserialize<IReadOnlyCollection<FileEntry>>(await File.ReadAllTextAsync(fileLocation));
        }
        void CheckEntries(IReadOnlyCollection<FileEntry> expectedCollection, IReadOnlyCollection<FileEntry> actualCollection, bool shouldHaveStream)
        {
            Assert.That(expectedCollection, Has.Count.EqualTo(actualCollection.Count));
            foreach (var expectedFileEntry in expectedCollection)
            {
                var actualFileEntry = actualCollection.FirstOrDefault(x => x.Location == expectedFileEntry.Location);

                Assert.That(actualFileEntry, Is.Not.Null);
                if (actualFileEntry == null)
                {
                    continue;
                }

                //Filename + Path indirectly get tested by above
                Assert.Multiple(() =>
                {
                    Assert.That(actualFileEntry.PreviousLocation, Is.EqualTo(expectedFileEntry.PreviousLocation));
                    Assert.That(actualFileEntry.Hash, Is.EqualTo(expectedFileEntry.Hash));
                    Assert.That(actualFileEntry.Filesize, Is.EqualTo(expectedFileEntry.Filesize));
                    Assert.That(actualFileEntry.Extension, Is.EqualTo(expectedFileEntry.Extension));
                    Assert.That(actualFileEntry.Stream, shouldHaveStream && expectedFileEntry.Filesize > 0 ? Is.Not.Null : Is.Null);
                });
            }
        }
    }

    [ExternalTest]
    public async Task TestFullPackageCreation(FullUpdatePackageTestData testData)
    {
        var location = Path.Combine("Assets", "Test Files", testData.SourceFolder);
        var packageLocation = Path.Combine("Assets", UpdatePackageCreatorName, testData.ApplicationName + "-" + "update_packages");
        
        FileSystem.Directory.CreateDirectory(packageLocation);
        
        var successful = await FullPackageCreator.CreateFullPackage(location, testData.Version, packageLocation, testData.ApplicationName);
        Assert.That(successful, Is.True);

        var expectedFileLocation = ExpectedTargetFileLocation(testData.ExpectedFilename);
        if (!FileSystem.File.Exists(expectedFileLocation))
        {
            Assert.Warn($"'{expectedFileLocation}' doesn't exist, unable to verify created update package (Test results might be inaccurate due to this)");
            return;
        }

        await using var targetStream = GetFullTargetStream(packageLocation, testData.ApplicationName, testData.Version);
        await using var expectedTargetStream = GetExpectedTargetStream(expectedFileLocation);

        try
        {
            CheckUpdatePackageWithExpected(targetStream, expectedTargetStream);
        }
        catch (NotImplementedException)
        {
            Assert.Warn("CheckUpdatePackageWithExpected isn't implemented, unable to verify created update package (Test results might be inaccurate due to this)");
        }
    }
    
    [ExternalTest]
    public async Task TestDeltaPackageCreation(DeltaUpdatePackageTestData testData)
    {
        FileSystem.Directory.CreateDirectory(Path.Combine("Assets", "Test Files", "EmptyFolder"));

        var packageLocation = Path.Combine("Assets", UpdatePackageCreatorName, testData.ApplicationName + "-" + "update_packages");
        var oldVersion = new SemanticVersion(1, 0, 0);

        var oldLocation = Path.Combine("Assets", "Test Files", testData.SourceFolder);
        var newLocation = Path.Combine("Assets", "Test Files", testData.TargetFolder);
        
        FileSystem.Directory.CreateDirectory(packageLocation);
        
        var successful = await DeltaPackageCreator.CreateDeltaPackage(oldLocation, oldVersion, newLocation, testData.NewVersion, packageLocation, testData.ApplicationName);
        Assert.That(successful, Is.True);

        var expectedFileLocation = ExpectedTargetFileLocation(testData.ExpectedFilename);
        if (!FileSystem.File.Exists(expectedFileLocation))
        {
            Assert.Warn($"'{expectedFileLocation}' doesn't exist, unable to verify created update package (Test results might be inaccurate due to this)");
            return;
        }

        await using var targetStream = GetDeltaTargetStream(packageLocation, testData.ApplicationName, testData.NewVersion);
        await using var expectedTargetStream = GetExpectedTargetStream(expectedFileLocation);

        try
        {
            CheckUpdatePackageWithExpected(targetStream, expectedTargetStream);
        }
        catch (NotImplementedException)
        {
            Assert.Warn("CheckUpdatePackageWithExpected isn't implemented, unable to verify created update package (Test results might be inaccurate due to this)");
        }
    }
    
    [Test]
    public async Task MakeFullUpdatePackage()
    {
        var version = new SemanticVersion(2, 0, 0);
        var applicationName = "testing";

        var location = Path.Combine("Assets", UpdatePackageCreatorName, applicationName + "-" + version);
        var packageLocation = Path.Combine("Assets", UpdatePackageCreatorName, applicationName + "-" + "update_packages");
        
        FileSystem.Directory.CreateDirectory(packageLocation);
        await CreateDirectoryData(location, 1);
        
        var successful = await FullPackageCreator.CreateFullPackage(location, version, packageLocation, applicationName);
        Assert.That(successful, Is.True);
        //Content checking is done by other tests, we just want to check if we can create a more complex update package
    }
    
    [Test]
    public async Task MakeDeltaUpdatePackage()
    {
        var oldVersion = new SemanticVersion(1, 0, 0);
        var newVersion = new SemanticVersion(1, 0, 1);
        var applicationName = "testing";

        var oldLocation = Path.Combine("Assets", UpdatePackageCreatorName, applicationName + "-" + oldVersion);
        var newLocation = Path.Combine("Assets", UpdatePackageCreatorName, applicationName + "-" + newVersion);
        var packageLocation = Path.Combine("Assets", UpdatePackageCreatorName, applicationName + "-" + "update_packages");
        
        FileSystem.Directory.CreateDirectory(packageLocation);
        if (FileSystem.Directory.Exists(newLocation))
        {
            FileSystem.Directory.Delete(newLocation, true);
        }

        await CreateDirectoryData(oldLocation);
        CopyDirectory(oldLocation, newLocation);
        await MessAroundWithDirectory(newLocation);
        
        var successful = await DeltaPackageCreator.CreateDeltaPackage(oldLocation, oldVersion, newLocation, newVersion, packageLocation, applicationName);
        Assert.That(successful, Is.True);
        //Content checking is done by other tests, we just want to check if we can create a more complex update package
    }
    
    protected abstract void CheckUpdatePackageWithExpected(Stream targetStream, Stream expectedTargetStream);
    
    protected Mock<IDeltaApplier> CreateMockDeltaApplier(string extension)
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


    protected Mock<IDeltaCreation> CreateMockDeltaCreation(string extension, double? filesizePercent = null)
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

    private async Task MessAroundWithDirectory(string directory)
    {
        var subDirs = FileSystem.Directory.GetDirectories(directory);
        FileSystem.Directory.Delete(subDirs[0], true);
        foreach (var subDir in subDirs.Skip(1))
        {
            var subDirFiles = FileSystem.Directory.GetFiles(subDir);
            var fileSwap1 = subDirFiles[1];
            var fileSwap2 = subDirFiles[2];
            
            //Swap the files over
            FileSystem.File.Move(fileSwap1, fileSwap1 + "2");
            FileSystem.File.Move(fileSwap2, fileSwap1);
            FileSystem.File.Move(fileSwap1 + "2", fileSwap2);

            foreach (var fileLocation in subDirFiles.Skip(2).Take(2))
            {
                await using var fileStream = FileSystem.File.Create(fileLocation);
                Functions.FillStreamWithRandomData(fileStream);
            }
            
            await MakeRandomFiles(subDir, 2);
        }
        
        await MakeRandomFiles(directory, 2);
        var dirFiles = FileSystem.Directory.GetFiles(directory);
        FileSystem.File.Move(dirFiles[0], Path.Combine(subDirs[1], Path.GetFileName(dirFiles[0])));
    }
    
    private void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        var sourceFiles = FileSystem.Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
        foreach (var sourceFile in sourceFiles)
        {
            var targetFile = Path.Combine(targetDirectory, Path.GetRelativePath(sourceDirectory, sourceFile));
            FileSystem.Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            
            FileSystem.File.Copy(sourceFile, targetFile);
        }
    }
    
    private async Task CreateDirectoryData(string directory, int? subDirCount = null)
    {
        if (FileSystem.Directory.Exists(directory))
        {
            FileSystem.Directory.Delete(directory, true);
        }
        
        subDirCount ??= Random.Shared.Next(2, 5);
        var subDirs = Enumerable.Range(0, subDirCount.Value)
            .Select(x => Path.Combine(directory, Path.GetRandomFileName()))
            .ToImmutableArray();

        //Also add the root directory so we get some more files
        foreach (var subDir in subDirs.Append(directory))
        {
            FileSystem.Directory.CreateDirectory(subDir);
            await MakeRandomFiles(subDir);
        }
    }

    private async Task MakeRandomFiles(string directory, int maxAmount = 15)
    {
        var filesCount = Random.Shared.Next(Math.Min(5, maxAmount), maxAmount);
        for (int i = 0; i < filesCount; i++)
        {
            var file = Path.Combine(directory, Path.GetRandomFileName());
            await MakeRandomFile(file);
        }
    }

    private async Task MakeRandomFile(string file)
    {
        await using var fileStream = FileSystem.File.OpenWrite(file);
        Functions.FillStreamWithRandomData(fileStream);
    }
    
    private string UpdatePackageCreatorName => DeltaPackageCreator.GetType().Name;
    
    private Stream GetDeltaTargetStream(string packageLocation, string applicationName, SemanticVersion newVersion)
    {
        return FileSystem.File.OpenRead(Path.Combine(packageLocation,
            string.Format(DeltaPackageCreator.DeltaPackageFilenameTemplate, applicationName, newVersion)));
    }

    private Stream GetFullTargetStream(string packageLocation, string applicationName, SemanticVersion newVersion)
    {
        return FileSystem.File.OpenRead(Path.Combine(packageLocation,
            string.Format(FullPackageCreator.FullPackageFilenameTemplate, applicationName, newVersion)));
    }

    private Stream GetExpectedTargetStream(string fileLocation) => FileSystem.File.OpenRead(fileLocation);

    private string ExpectedTargetFileLocation(string filename) => Path.Combine("Assets", UpdatePackageCreatorName,
        filename + DeltaPackageCreator.Extension);
}