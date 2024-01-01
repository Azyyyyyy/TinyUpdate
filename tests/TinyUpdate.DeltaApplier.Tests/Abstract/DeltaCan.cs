using System.ComponentModel;
using System.Runtime.InteropServices;
using TinyUpdate.Core.Abstract;

namespace TinyUpdate.DeltaApplier.Tests.Abstract;

/// <summary>
/// Provides base tests for any <see cref="IDeltaApplier"/> and <see cref="IDeltaCreation"/>
/// </summary>
[NUnit.Framework.Category("Delta")]
public abstract class DeltaCan
{
    protected IDeltaApplier Applier = null!; //The actual test will take care of creating these TODO: Skip if these are null?
    protected IDeltaCreation Creation = null!;

    //TODO: TargetStreamSize Test
    
    [Test]
    [TestCase("expected.diff", true)]
    [NUnit.Framework.Category("Delta Applier")]
    public void GetCorrectSupportStatus(string file, bool expectedResult)
    {
        var deltaFile = File.OpenRead(Path.Combine("Assets", Applier.GetType().Name, file));
        var status = Applier.SupportedStream(deltaFile);
        var error = GetWin32Error();
        
        Assert.That(status, Is.EqualTo(expectedResult), () => $"Error thrown: {error?.Message} (ErrorCode: {error?.NativeErrorCode})");
    }
    
    [Test]
    [NUnit.Framework.Category("Delta Creation")]
    public async Task CreateDeltaFile()
    {
        var sourceFile = File.OpenRead(Path.Combine("Assets", "original.jpg"));
        var targetFile = File.OpenRead(Path.Combine("Assets", "new.jpg"));
        var deltaFile = File.Create(Path.Combine("Assets", Creation.GetType().Name, "result.diff"));
        
        var result = await Creation.CreateDeltaFile(sourceFile, targetFile, deltaFile);
        var error = GetWin32Error();
        
        Assert.That(result, Is.True, () => $"Error thrown: {error?.Message} (ErrorCode: {error?.NativeErrorCode})");
        //TODO: Check that the file matches what we would expect to be returned
    }
    
    [Test]
    [NUnit.Framework.Category("Delta Applier")]
    public async Task ApplyDeltaFile()
    {
        var sourceFile = File.OpenRead(Path.Combine("Assets", "original.jpg"));
        var deltaFile = File.OpenRead(Path.Combine("Assets", Applier.GetType().Name, "expected.diff"));
        var targetFile = File.Create(Path.Combine("Assets", Applier.GetType().Name, "new (diff).jpg"));
        
        var result = await Applier.ApplyDeltaFile(sourceFile, deltaFile, targetFile);
        var error = GetWin32Error();

        Assert.That(result, Is.True, () => $"Error thrown: {error?.Message} (ErrorCode: {error?.NativeErrorCode})");
        //TODO: Check if "new (diff)" is the same as "new"
    }

    private static Win32Exception? GetWin32Error()
    {
        var win32Error = Marshal.GetLastWin32Error();
        return win32Error != 0 ? new Win32Exception(win32Error) : null;
    }
}