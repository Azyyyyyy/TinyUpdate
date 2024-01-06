using System.Collections.Immutable;
using System.IO.Abstractions;
using Moq;
using SemVersion;
using TinyUpdate.Core.Abstract;
using TinyUpdate.Packages.Tests.Attributes;
using TinyUpdate.Tests.Common;

namespace TinyUpdate.Packages.Tests.Abstract;

public abstract class UpdatePackageCan
{
    protected IUpdatePackage UpdatePackage;
    protected IUpdatePackageCreator UpdatePackageCreator;
    protected IFileSystem FileSystem = null!;

    [OneTimeSetUp]
    public void BaseSetup()
    {
        FileSystem = Functions.SetupMockFileSystem();
    }

    [Test]
    public async Task CreateUpdatePackageWithNewFileInRoot()
    {
        var version = new SemanticVersion(2, 0, 0);
        var applicationName = "new-application";
        var packageLocation = await ProcessFullPackage("NewRootFiles", applicationName, version);
        
        await using var targetFileStream = GetFullTargetFileStream(packageLocation, applicationName, version);
        await using var expectedTargetFileStream = GetExpectedTargetFileStream("newFileRoot");

        CheckNewFileInRootUpdatePackage(targetFileStream, expectedTargetFileStream);
    }
    
    [Test]
    public async Task CreateUpdatePackageWithNewFileInSubDir()
    {
        var version = new SemanticVersion(2, 0, 0);
        var applicationName = "new-application";
        var packageLocation = await ProcessFullPackage("NewSubdirFiles", applicationName, version);

        await using var targetFileStream = GetFullTargetFileStream(packageLocation, applicationName, version);
        await using var expectedTargetFileStream = GetExpectedTargetFileStream("newFileSubdir");

        CheckNewFileInSubDirUpdatePackage(targetFileStream, expectedTargetFileStream);
    }
    
    [Test]
    [FixedCreatorSize]
    public async Task CreateUpdatePackageWithDeltaFileInRoot()
    {
        var newVersion = new SemanticVersion(1, 0, 1);
        var applicationName = "new-application";
        var packageLocation = await ProcessDeltaPackage("NewRootFiles", "DeltaRootFiles", applicationName, newVersion);
        
        await using var targetFileStream = GetDeltaTargetFileStream(packageLocation, applicationName, newVersion);
        await using var expectedTargetFileStream = GetExpectedTargetFileStream("deltaFileRoot");

        CheckDeltaFileInRootUpdatePackage(targetFileStream, expectedTargetFileStream);
    }
    
    [Test]
    [FixedCreatorSize]
    public async Task CreateUpdatePackageWithDeltaFileInSubDir()
    {
        var newVersion = new SemanticVersion(1, 0, 1);
        var applicationName = "new-application";
        var packageLocation = await ProcessDeltaPackage("NewSubdirFiles", "DeltaSubdirFiles", applicationName, newVersion);
        
        await using var targetFileStream = GetDeltaTargetFileStream(packageLocation, applicationName, newVersion);
        await using var expectedTargetFileStream = GetExpectedTargetFileStream("deltaFileSubdir");

        CheckDeltaFileInSubDirUpdatePackage(targetFileStream, expectedTargetFileStream);
    }
    
    [Test]
    public async Task CreateUpdatePackageWithMovedFileInRoot()
    {
        var newVersion = new SemanticVersion(1, 0, 1);
        var applicationName = "new-application";
        var packageLocation = await ProcessDeltaPackage("NewRootFiles", "MovedRootFiles", applicationName, newVersion);
        
        await using var targetFileStream = GetDeltaTargetFileStream(packageLocation, applicationName, newVersion);
        await using var expectedTargetFileStream = GetExpectedTargetFileStream("movedFileRoot");

        CheckMovedFileInRootUpdatePackage(targetFileStream, expectedTargetFileStream);
    }

    [Test]
    public async Task CreateUpdatePackageWithMovedFileRootToSubDir()
    {
        var newVersion = new SemanticVersion(1, 0, 1);
        var applicationName = "new-application";
        var packageLocation = await ProcessDeltaPackage("NewRootFiles", "MovedRootToSubdirFiles", applicationName, newVersion);

        await using var targetFileStream = GetDeltaTargetFileStream(packageLocation, applicationName, newVersion);
        await using var expectedTargetFileStream = GetExpectedTargetFileStream("movedFileRootToSubdir");

        CheckMovedFileRootToSubDirUpdatePackage(targetFileStream, expectedTargetFileStream);
    }
    
    [Test]
    public async Task CreateUpdatePackageWithMovedFileSubDirToRoot()
    {
        var newVersion = new SemanticVersion(1, 0, 1);
        var applicationName = "new-application";
        var packageLocation = await ProcessDeltaPackage("NewSubdirFiles", "MovedSubdirToRootFiles", applicationName, newVersion);

        await using var targetFileStream = GetDeltaTargetFileStream(packageLocation, applicationName, newVersion);
        await using var expectedTargetFileStream = GetExpectedTargetFileStream("movedFileSubdirToRoot");

        CheckMovedFileSubDirToRootUpdatePackage(targetFileStream, expectedTargetFileStream);
    }

    //TODO: Add UnchangedTests
    
    [Test]
    public async Task CreateUpdatePackageWithMovedFileSubDirToSubDir()
    {
        var newVersion = new SemanticVersion(1, 0, 1);
        var applicationName = "new-application";
        var packageLocation = await ProcessDeltaPackage("NewSubdirFiles", "MovedSubdirToSubdirFiles", applicationName, newVersion);
        
        await using var targetFileStream = GetDeltaTargetFileStream(packageLocation, applicationName, newVersion);
        await using var expectedTargetFileStream = GetExpectedTargetFileStream("movedFileSubdirToSubdir");

        CheckMovedFileSubDirToSubDirUpdatePackage(targetFileStream, expectedTargetFileStream);
    }
    
    [Test]
    public async Task ProcessFileData()
    {
        var fileStream = FileSystem.File.OpenRead(Path.Combine("Assets", UpdatePackage.GetType().Name, "testing-1.0.0" + UpdatePackage.Extension));
        await UpdatePackage.Load(fileStream);
        //TODO: Check contents is as expected
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
        
        var successful = await UpdatePackageCreator.CreateFullPackage(location, version, packageLocation, applicationName);
        Assert.That(successful, Is.True);
        //TODO: Check contents is as expected
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
        
        var successful = await UpdatePackageCreator.CreateDeltaPackage(oldLocation, oldVersion, newLocation, newVersion, packageLocation, applicationName);
        Assert.That(successful, Is.True);
        //TODO: Check contents is as expected
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

    private async Task<string> ProcessFullPackage(string sourceFolder, string applicationName, SemanticVersion version)
    {
        var location = Path.Combine("Assets", "Test Files", sourceFolder);
        var packageLocation = Path.Combine("Assets", UpdatePackageCreatorName, applicationName + "-" + "update_packages");
        
        FileSystem.Directory.CreateDirectory(packageLocation);
        
        var successful = await UpdatePackageCreator.CreateFullPackage(location, version, packageLocation, applicationName);
        Assert.That(successful, Is.True);
        return packageLocation;
    }
    
    private async Task<string> ProcessDeltaPackage(string sourceFolder, string targetFolder, string applicationName, SemanticVersion newVersion)
    {
        var packageLocation = Path.Combine("Assets", UpdatePackageCreatorName, applicationName + "-" + "update_packages");
        var oldVersion = new SemanticVersion(1, 0, 0);

        var oldLocation = Path.Combine("Assets", "Test Files", sourceFolder);
        var newLocation = Path.Combine("Assets", "Test Files", targetFolder);
        
        FileSystem.Directory.CreateDirectory(packageLocation);
        
        var successful = await UpdatePackageCreator.CreateDeltaPackage(oldLocation, oldVersion, newLocation, newVersion, packageLocation, applicationName);
        Assert.That(successful, Is.True);

        return packageLocation;
    }
    
    private string UpdatePackageCreatorName => UpdatePackageCreator.GetType().Name;

    protected abstract void CheckNewFileInRootUpdatePackage(Stream targetFileStream, Stream expectedTargetFileStream);
    protected abstract void CheckNewFileInSubDirUpdatePackage(Stream targetFileStream, Stream expectedTargetFileStream);
    protected abstract void CheckDeltaFileInRootUpdatePackage(Stream targetFileStream, Stream expectedTargetFileStream);
    protected abstract void CheckDeltaFileInSubDirUpdatePackage(Stream targetFileStream, Stream expectedTargetFileStream);
    protected abstract void CheckMovedFileInRootUpdatePackage(Stream targetFileStream, Stream expectedTargetFileStream);
    protected abstract void CheckMovedFileRootToSubDirUpdatePackage(Stream targetFileStream, Stream expectedTargetFileStream);
    protected abstract void CheckMovedFileSubDirToRootUpdatePackage(Stream targetFileStream, Stream expectedTargetFileStream);
    protected abstract void CheckMovedFileSubDirToSubDirUpdatePackage(Stream targetFileStream, Stream expectedTargetFileStream);
    
    protected Mock<IDeltaApplier> CreateMockDeltaApplier(string extension)
    {
        var mockApplier = new Mock<IDeltaApplier>();
        
        mockApplier.Setup(x => x.Extension).Returns(extension);
        mockApplier.Setup(x => x.SupportedStream(It.IsAny<Stream>())).Returns(true);
        mockApplier.Setup(x => 
                x.ApplyDeltaFile(It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<IProgress<double>>()))
            .Callback((Stream sourceFileStream, Stream deltaFileStream, Stream targetFileStream,
                IProgress<double>? progress) => Functions.FillStreamWithRandomData(deltaFileStream))
            .ReturnsAsync(true);

        return mockApplier;
    }


    protected Mock<IDeltaCreation> CreateMockDeltaCreation(string extension, double? filesizePercent = null)
    {
        var mockCreation = new Mock<IDeltaCreation>();

        mockCreation.Setup(x => x.Extension).Returns(extension);
        mockCreation.Setup(x => 
                x.CreateDeltaFile(It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<IProgress<double>>()))
            .Callback((Stream sourceFileStream, Stream targetFileStream, Stream deltaFileStream,
                IProgress<double>? progress) =>
            {
                filesizePercent ??= Random.Shared.NextDouble();
                var filesize = (long)(targetFileStream.Length * filesizePercent);

                Functions.FillStreamWithRandomData(deltaFileStream, filesize);
            }).ReturnsAsync(true);
        
        return mockCreation;
    }
    
    protected Stream GetDeltaTargetFileStream(string packageLocation, string applicationName, SemanticVersion newVersion)
    {
        return FileSystem.File.OpenRead(Path.Combine(packageLocation,
            string.Format(UpdatePackageCreator.DeltaPackageFilenameTemplate, applicationName, newVersion)));
    }

    protected Stream GetFullTargetFileStream(string packageLocation, string applicationName, SemanticVersion newVersion)
    {
        return FileSystem.File.OpenRead(Path.Combine(packageLocation,
            string.Format(UpdatePackageCreator.FullPackageFilenameTemplate, applicationName, newVersion)));
    }

    protected Stream GetExpectedTargetFileStream(string filenamae)
    {
        return FileSystem.File.OpenRead(Path.Combine("Assets", UpdatePackageCreatorName,
            filenamae + UpdatePackageCreator.Extension));
    }
}