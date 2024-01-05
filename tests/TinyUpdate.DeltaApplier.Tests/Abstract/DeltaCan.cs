using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Abstractions;
using TinyUpdate.Core;
using TinyUpdate.Core.Abstract;
using TinyUpdate.Tests.Common.Attributes;

namespace TinyUpdate.DeltaApplier.Tests.Abstract;

/// <summary>
/// Provides base tests for any <see cref="IDeltaApplier"/> and <see cref="IDeltaCreation"/>
/// </summary>
[NUnit.Framework.Category("Delta")]
public abstract class DeltaCan
{
    protected IDeltaApplier? Applier; //The actual test will take care of creating these
    protected IDeltaCreation? Creator;
    protected IFileSystem FileSystem;
    protected readonly SHA256 Sha256 = new SHA256(NullLogger.Instance);

    protected string ApplierName => Applier?.GetType().Name ?? "N/A";
    protected string CreatorName => Creator?.GetType().Name ?? "N/A";

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
    
    //TODO: TargetStreamSize Test
    
    [Test]
    [DeltaApplier]
    [TestCase("expected_pass", true)]
    [TestCase("expected_fail", false)]
    public void GetCorrectSupportStatus(string targetFilename, bool expectedResult)
    {
        SkipIfNoApplier();
        
        using var deltaFileStream = FileSystem.File.OpenRead(Path.Combine("Assets", ApplierName, targetFilename + Applier.Extension));
        var returnedStatus = Applier.SupportedStream(deltaFileStream);
        var error = GetWin32Error();
        
        Assert.That(returnedStatus, Is.EqualTo(expectedResult), () => CreateErrorMessage(error));
    }
    
    [Test]
    [DeltaApplier]
    public async Task CorrectlyApplyDeltaFile()
    {
        SkipIfNoApplier();

        await using var sourceFileStream = FileSystem.File.OpenRead(Path.Combine("Assets", "original.jpg"));
        await using var deltaFileStream = FileSystem.File.OpenRead(Path.Combine("Assets", ApplierName, "expected_pass" + Applier.Extension));
        await using var targetFileStream = FileSystem.File.Create(Path.Combine("Assets", ApplierName, "new (diff).jpg"));
        await using var expectedTargetFileStream = FileSystem.File.OpenRead(Path.Combine("Assets", "new.jpg"));
        
        var applyResult = await Applier.ApplyDeltaFile(sourceFileStream, deltaFileStream, targetFileStream);
        var error = GetWin32Error();
        targetFileStream.Seek(0, SeekOrigin.Begin);

        var expectedTargetFileStreamHash = Sha256.CreateSHA256Hash(expectedTargetFileStream);
        var targetFileStreamHash = Sha256.CreateSHA256Hash(targetFileStream);
        Assert.Multiple(() =>
        {
            Assert.That(applyResult, Is.True, () => CreateErrorMessage(error));
            Assert.That(expectedTargetFileStreamHash, Is.EqualTo(targetFileStreamHash), () => $"{ApplierName} didn't create an exact replica of the target stream. Expected Hash: {expectedTargetFileStreamHash}, Target Hash: {targetFileStreamHash}");
        });
    }

    [Test]
    [DeltaCreation]
    public async Task CreateDeltaFile()
    {
        SkipIfNoCreator();

        await using var sourceFileStream = FileSystem.File.OpenRead(Path.Combine("Assets", "original.jpg"));
        await using var targetFileStream = FileSystem.File.OpenRead(Path.Combine("Assets", "new.jpg"));
        await using var deltaFileStream = FileSystem.File.Create(Path.Combine("Assets", CreatorName, "result_delta" + Creator.Extension));
        
        var createResult = await Creator.CreateDeltaFile(sourceFileStream, targetFileStream, deltaFileStream);
        var error = GetWin32Error();
        
        Assert.That(createResult, Is.True, () => CreateErrorMessage(error));
        //TODO: Check that the file matches what we would expect to be returned
    }
    
    [MemberNotNull(nameof(Applier))]
    private void SkipIfNoApplier()
    {
        if (Applier == null)
        {
            Assert.Ignore("Applier has not been setup, can't run test");
        }
    }
    
    [MemberNotNull(nameof(Creator))]
    private void SkipIfNoCreator()
    {
        if (Creator == null)
        {
            Assert.Ignore("Creator has not been setup, can't run test");
        }
    }
    
    private static Win32Exception? GetWin32Error()
    {
        var win32Error = Marshal.GetLastWin32Error();
        return win32Error != 0 ? new Win32Exception(win32Error) : null;
    }
    
    private static string CreateErrorMessage(Win32Exception? error) =>
        error != null 
            ? $"Error thrown: {error?.Message} (ErrorCode: {error?.NativeErrorCode})"
            : "Unknown Error";
}