using System.IO.Abstractions;
using Moq;
using TinyUpdate.Appliers.Tests.Models;
using TinyUpdate.Appliers.Tests.TestSources;
using TinyUpdate.Core;
using TinyUpdate.Core.Abstract;
using TinyUpdate.Core.Abstract.Delta;
using TinyUpdate.Core.Abstract.UpdatePackage;
using TinyUpdate.Core.Model;
using TinyUpdate.Core.Services;
using TinyUpdate.Core.Tests;
using TinyUpdate.Desktop;
using TinyUpdate.Desktop.Abstract;

namespace TinyUpdate.Appliers.Tests;

public class DesktopTests
{
    private IUpdateApplier _updateApplier = null!;
    private IFileSystem _fileSystem = null!;
    private IHasher _hasher = null!;
    private IDeltaManager _deltaManager = null!;
    private MockNative _mockNative = null!;

    [SetUp]
    public void Setup()
    {
        _hasher = SHA256.Instance;
        _deltaManager = DeltaMocker.CreateDeltaManager(false, CreateMockDeltaApplier);
        _fileSystem = Functions.SetupMockFileSystem();
        _mockNative = new MockNative(_fileSystem);
        _updateApplier = new DesktopApplier(NUnitLogger<DesktopApplier>.Instance, _hasher, _mockNative, _deltaManager, _fileSystem);
    }
    
    [Test]
    public async Task CanApplyUpdates() {
        var applicationPath = Path.Combine("Assets", nameof(DesktopApplier));
        var appV0Path = Path.Combine(applicationPath, "1.0.0");
        var appV1Path = Path.Combine(applicationPath, "1.0.1");
        var appV2Path = Path.Combine(applicationPath, "1.0.2");

        var appV1 = new MockUpdatePackage(applicationPath, _fileSystem)
        {
            ReleaseEntry = new MockReleaseEntry("1.0.0", "1.0.1", true),
            NewFiles = [DesktopApplierTestSource.MakeFileEntry("testing.exe", appV0Path)]
        };
        var appV2 = new MockUpdatePackage(applicationPath, _fileSystem)
        {
            ReleaseEntry = new MockReleaseEntry("1.0.1", "1.0.2", true),
            NewFiles = [DesktopApplierTestSource.MakeFileEntry(Path.Combine("testsub", "testApplication.exe"), appV1Path)]
        };
        var appV3 = new MockUpdatePackage(applicationPath, _fileSystem)
        {
            ReleaseEntry = new MockReleaseEntry("1.0.2", "1.0.3", true),
            NewFiles = [DesktopApplierTestSource.MakeFileEntry(Path.Combine("newsub", "testApplication.exe"), appV2Path)]
        };

        var result = await _updateApplier.ApplyUpdates([appV1, appV3, appV2], applicationPath);
        Assert.That(result, Is.True);
    }
    
    [Test]
    [TestCaseSource(typeof(DesktopApplierTestSource), nameof(DesktopApplierTestSource.TestS))]
    public async Task CanApplyUpdate(ApplierTestData applierTestData)
    {
        applierTestData.UpdatePackage.Setup();
        
        var success = await _updateApplier.ApplyUpdate(applierTestData.UpdatePackage, applierTestData.Location);
        Assert.That(success, Is.True);

        var newVersion = applierTestData.UpdatePackage.ReleaseEntry.NewVersion;
        var newVersionPath = Path.Combine(applierTestData.Location, newVersion.ToString());

        var newVersionAbPath = Path.Combine(_fileSystem.Directory.GetCurrentDirectory(), newVersionPath);
        
        var storedApplicationFiles = _fileSystem.Directory.GetFiles(newVersionPath, "*", SearchOption.AllDirectories);
        var expectedApplicationFiles = new List<string>();

        Assert.That(storedApplicationFiles.Length, Is.EqualTo(applierTestData.UpdatePackage.FileCount));
        foreach (var fileEntry in applierTestData.UpdatePackage.GetAllEntries())
        {
            var filePath = Path.Combine(newVersionPath, fileEntry.Location);
            expectedApplicationFiles.Add(fileEntry.Location);
            if (!_fileSystem.File.Exists(filePath))
            {
                Assert.Fail($"{filePath} doesn't exist when it's expected");
            }
            
            if (!string.IsNullOrWhiteSpace(fileEntry.PreviousLocation))
            {
                Assert.That(_mockNative.ProcessedFiles, Contains.Item(filePath));
            }

            await using var fileStream = _fileSystem.File.OpenRead(filePath);
            Assert.Multiple(() =>
            {
                Assert.That(fileStream.Length, Is.EqualTo(fileEntry.Filesize));
                Assert.That(_hasher.HashData(fileStream), Is.EqualTo(fileEntry.Hash));
            });
        }
        
        Assert.That(storedApplicationFiles.Select(x => Path.GetRelativePath(newVersionAbPath, x)).Order(), Is.EquivalentTo(expectedApplicationFiles.Order()));
    }
    
    public static Mock<IDeltaApplier> CreateMockDeltaApplier(string extension)
    {
        var mockApplier = new Mock<IDeltaApplier>();
        
        mockApplier.Setup(x => x.Extension).Returns(extension);
        mockApplier.Setup(x => x.SupportedStream(It.IsAny<Stream>())).Returns(true);
        mockApplier.Setup(x => 
                x.ApplyDeltaFile(It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<IProgress<double>>()))
            .Callback((Stream sourceStream, Stream deltaStream, Stream targetStream,
                IProgress<double>? progress) => deltaStream.CopyToAsync(targetStream))
            .ReturnsAsync(true);

        return mockApplier;
    }
    
    private class MockNative(IFileSystem fileSystem) : INative
    {
        public readonly List<string> ProcessedFiles = [];
        
        public bool CreateHardLink(string sourcePath, string targetPath)
        {
            fileSystem.File.Copy(sourcePath, targetPath);
            ProcessedFiles.Add(targetPath);
            return true;
        }
    }
}

internal static class UpdatePackageExtension
{
    public static IEnumerable<FileEntry> GetAllEntries(this IUpdatePackage updatePackage)
    {
        return updatePackage.DeltaFiles.Concat(updatePackage.UnchangedFiles).Concat(updatePackage.NewFiles)
            .Concat(updatePackage.MovedFiles);
    }
}