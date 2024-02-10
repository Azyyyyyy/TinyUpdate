using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using TinyUpdate.Core;
using TinyUpdate.Core.Abstract;
using TinyUpdate.Core.Abstract.Delta;
using TinyUpdate.Core.Services;
using TinyUpdate.Core.Tests;
using TinyUpdate.Core.Tests.Attributes;

namespace TinyUpdate.Delta.Tests.Abstract;

/// <summary>
/// Provides base tests for any <see cref="IDeltaApplier"/> and <see cref="IDeltaCreation"/>
/// </summary>
[NUnit.Framework.Category("Delta")]
public abstract class DeltaCan
{
    protected IDeltaApplier? Applier; //The actual test will take care of creating these
    protected IDeltaCreation? Creator;
    protected IFileSystem FileSystem;
    protected readonly IHasher Hasher = SHA256.Instance;

    protected string ApplierName => Applier?.GetType().Name ?? "N/A";
    protected string CreatorName => Creator?.GetType().Name ?? "N/A";

    [OneTimeSetUp]
    public void BaseSetup()
    {
        FileSystem = Functions.SetupMockFileSystem();
    }

    //TODO: Add test for handling streams over the array size
    
    [Test]
    public void CanGetCorrectTargetStreamSize()
    {
        SkipIfNoApplier();

        using var deltaStream = FileSystem.File.OpenRead(Path.Combine("Assets", ApplierName, "expected_pass" + Applier.Extension));
        var targetSize = Applier.TargetStreamSize(deltaStream);
        
        Assert.That(targetSize, Is.EqualTo(233237));
    }
    
    [Test]
    [DeltaApplier]
    [TestCase("expected_pass", true)]
    [TestCase("expected_fail", false)]
    public void GetCorrectSupportStatus(string targetFilename, bool expectedResult)
    {
        SkipIfNoApplier();
        
        using var deltaStream = FileSystem.File.OpenRead(Path.Combine("Assets", ApplierName, targetFilename + Applier.Extension));
        var returnedStatus = Applier.SupportedStream(deltaStream);
        var error = GetWin32Error();
        
        Assert.That(returnedStatus, Is.EqualTo(expectedResult), () => CreateErrorMessage(error));
    }
    
    [Test]
    [DeltaApplier]
    public async Task CorrectlyApplyDeltaFile()
    {
        SkipIfNoApplier();

        await using var sourceStream = FileSystem.File.OpenRead(Path.Combine("Assets", "original.jpg"));
        await using var deltaStream = FileSystem.File.OpenRead(Path.Combine("Assets", ApplierName, "expected_pass" + Applier.Extension));
        await using var targetStream = FileSystem.File.Create(Path.Combine("Assets", ApplierName, "new (diff).jpg"));
        await using var expectedTargetStream = FileSystem.File.OpenRead(Path.Combine("Assets", "new.jpg"));
        
        var applyResult = await Applier.ApplyDeltaFile(sourceStream, deltaStream, targetStream);
        var error = GetWin32Error();
        targetStream.Seek(0, SeekOrigin.Begin);

        var expectedTargetStreamHash = Hasher.HashData(expectedTargetStream);
        var targetStreamHash = Hasher.HashData(targetStream);
        Assert.Multiple(() =>
        {
            Assert.That(applyResult, Is.True, () => CreateErrorMessage(error));
            Assert.That(expectedTargetStreamHash, Is.EqualTo(targetStreamHash), () => $"{ApplierName} didn't create an exact replica of the target stream. Expected Hash: {expectedTargetStreamHash}, Target Hash: {targetStreamHash}");
        });
    }

    [Test]
    [DeltaCreation]
    public async Task CreateDeltaFile()
    {
        SkipIfNoCreator();

        await using var sourceStream = FileSystem.File.OpenRead(Path.Combine("Assets", "original.jpg"));
        await using var targetStream = FileSystem.File.OpenRead(Path.Combine("Assets", "new.jpg"));
        await using var deltaStream = FileSystem.File.Create(Path.Combine("Assets", CreatorName, "result_delta" + Creator.Extension));
        
        var createResult = await Creator.CreateDeltaFile(sourceStream, targetStream, deltaStream);
        var error = GetWin32Error();
        
        Assert.That(createResult, Is.True, () => CreateErrorMessage(error));

        deltaStream.Seek(0, SeekOrigin.Begin);
        await using var expectedDeltaStream = FileSystem.File.OpenRead(Path.Combine("Assets", CreatorName, "expectedDelta" + Creator.Extension));
        
        CheckDeltaFile(deltaStream, expectedDeltaStream);
    }
    
    protected abstract void CheckDeltaFile(Stream targetHashStream, Stream expectedTargetHashStream);

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
            ? $"Error thrown: {error.Message} (ErrorCode: {error.NativeErrorCode})"
            : "Unknown Error";
}