using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using TinyUpdate.Core.Abstract;
using TinyUpdate.DeltaApplier.Tests.Attributes;

namespace TinyUpdate.DeltaApplier.Tests.Abstract;

/// <summary>
/// Provides base tests for any <see cref="IDeltaApplier"/> and <see cref="IDeltaCreation"/>
/// </summary>
[NUnit.Framework.Category("Delta")]
public abstract class DeltaCan
{
    protected IDeltaApplier? Applier; //The actual test will take care of creating these
    protected IDeltaCreation? Creator;
    protected readonly IFile File = new FileWrapper(new FileSystem());

    protected string ApplierName => Applier?.GetType().Name ?? "N/A";
    protected string CreatorName => Creator?.GetType().Name;
    
    //TODO: TargetStreamSize Test
    
    [Test]
    [DeltaApplier]
    [TestCase("expected_pass", true)]
    [TestCase("expected_fail", false)]
    public void GetCorrectSupportStatus(string targetFilename, bool expectedResult)
    {
        SkipIfNoApplier();
        
        var deltaFileStream = File.OpenRead(Path.Combine("Assets", ApplierName, targetFilename + Applier.Extension));
        var returnedStatus = Applier.SupportedStream(deltaFileStream);
        var error = GetWin32Error();
        
        Assert.That(returnedStatus, Is.EqualTo(expectedResult), () => CreateErrorMessage(error));
    }
    
    [Test]
    [DeltaApplier]
    public async Task CorrectlyApplyDeltaFile()
    {
        SkipIfNoApplier();

        var sourceFileStream = File.OpenRead(Path.Combine("Assets", "original.jpg"));
        var deltaFileStream = File.OpenRead(Path.Combine("Assets", ApplierName, "expected_pass" + Applier.Extension));
        var targetFileStream = File.Create(Path.Combine("Assets", ApplierName, "new (diff).jpg"));
        
        var applyResult = await Applier.ApplyDeltaFile(sourceFileStream, deltaFileStream, targetFileStream);
        var error = GetWin32Error();

        Assert.That(applyResult, Is.True, () => CreateErrorMessage(error));
        //TODO: Check if "new (diff)" is the same as "new"
    }
    
    [Test]
    [DeltaCreation]
    public async Task CreateDeltaFile()
    {
        SkipIfNoCreator();

        var sourceFileStream = File.OpenRead(Path.Combine("Assets", "original.jpg"));
        var targetFileStream = File.OpenRead(Path.Combine("Assets", "new.jpg"));
        var deltaFileStream = File.Create(Path.Combine("Assets", CreatorName, "result_delta" + Creator.Extension));
        
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
    
    private static string CreateErrorMessage(Win32Exception? error) => $"Error thrown: {error?.Message} (ErrorCode: {error?.NativeErrorCode})";
}