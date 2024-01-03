using System.Collections.Immutable;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using SemVersion;
using TinyUpdate.Core.Abstract;

namespace TinyUpdate.Packages.Tests.Abstract;

public abstract class UpdatePackageCan
{
    protected IUpdatePackage UpdatePackage;
    protected IUpdatePackageCreator UpdatePackageCreator;
    protected IFileSystem FileSystem;

    [OneTimeSetUp]
    public void BaseSetup()
    {
        var fileSystem = new MockFileSystem(new MockFileSystemOptions
        {
            CurrentDirectory = Environment.CurrentDirectory,
            CreateDefaultTempDir = false
        });
        FileSystem = fileSystem;

        var files = Directory.GetFiles("Assets", "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            fileSystem.AddFile(file, new MockFileData(File.ReadAllBytes(file)));
        }
    }

    [Test]
    public async Task CanProcessFileData()
    {
        var fileStream = FileSystem.File.OpenRead(Path.Combine("Assets", UpdatePackage.GetType().Name, "testing-1.0.0.tuup"));
        await UpdatePackage.Load(fileStream);
        //TODO: Check contents is as expected
    }
    
    [Test]
    public async Task CanMakeFullUpdatePackage()
    {
        var version = new SemanticVersion(2, 0, 0);
        var applicationName = "testing";

        var location = Path.Combine("Assets", UpdatePackageCreator.GetType().Name, applicationName + "-" + version);
        var packageLocation = Path.Combine("Assets", UpdatePackageCreator.GetType().Name, applicationName + "-" + "update_packages");
        
        FileSystem.Directory.CreateDirectory(packageLocation);
        await CreateDirectoryData(location);
        
        var successful = await UpdatePackageCreator.CreateFullPackage(location, version, packageLocation, applicationName);
        Assert.That(successful, Is.True);
        //TODO: Check contents is as expected
    }
    
    [Test]
    public async Task CanMakeDeltaUpdatePackage()
    {
        var oldVersion = new SemanticVersion(1, 0, 0);
        var newVersion = new SemanticVersion(1, 0, 1);
        var applicationName = "testing";

        var oldLocation = Path.Combine("Assets", UpdatePackageCreator.GetType().Name, applicationName + "-" + oldVersion);
        var newLocation = Path.Combine("Assets", UpdatePackageCreator.GetType().Name, applicationName + "-" + newVersion);
        var packageLocation = Path.Combine("Assets", UpdatePackageCreator.GetType().Name, applicationName + "-" + "update_packages");
        
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
        //TODO: moved files across directories is currently not working
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
    
    private async Task CreateDirectoryData(string directory)
    {
        if (FileSystem.Directory.Exists(directory))
        {
            FileSystem.Directory.Delete(directory, true);
        }
        
        var subdirCount = Random.Shared.Next(2, 5);
        var subdirs = Enumerable.Range(0, subdirCount).Select(x => Path.Combine(directory, Path.GetRandomFileName())).ToImmutableArray();

        foreach (var subdir in subdirs.Append(directory))
        {
            FileSystem.Directory.CreateDirectory(subdir);
            await MakeRandomFiles(subdir);
        }
    }

    private async Task MakeRandomFiles(string directory, int maxAmount = 15)
    {
        var filesCount = Random.Shared.Next(Math.Min(3, maxAmount), maxAmount);
        for (int i = 0; i < filesCount; i++)
        {
            var file = Path.Combine(directory, Path.GetRandomFileName());
            await MakeRandomFile(file);
        }
    }

    private async Task MakeRandomFile(string file)
    {
        await using var fileStream = FileSystem.File.OpenWrite(file);

        var filesize = Random.Shared.Next(1024 * 1000);
        var buffer = new byte[filesize];
        Random.Shared.NextBytes(buffer);

        await fileStream.WriteAsync(buffer);
    }
}