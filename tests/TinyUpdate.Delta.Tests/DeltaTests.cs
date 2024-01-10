using Microsoft.Extensions.Logging.Abstractions;
using TinyUpdate.Delta.Tests.Abstract;
using TinyUpdate.Delta.MSDelta.Struct;
using TinyUpdate.Core.Tests.Attributes;
using TinyUpdate.Delta.BSDiff;

namespace TinyUpdate.Delta.Tests;

public class BSDiffDeltaTests : DeltaCan
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var delta = new BSDiffDelta(NullLogger.Instance);
        Creator = delta;
        Applier = delta;
    }

    protected override void CheckDeltaFile(Stream targetFileStream, Stream expectedTargetFileStream)
    {
        var expectedTargetFileStreamHash = Hasher.HashData(expectedTargetFileStream);
        var targetFileStreamHash = Hasher.HashData(targetFileStream);
        Assert.That(expectedTargetFileStreamHash, Is.EqualTo(targetFileStreamHash), () => $"{ApplierName} delta file is not as expected");
    }
}

[NonWindowsIgnore]
public class MSDeltaTests : DeltaCan
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        //We don't actually need this but makes warnings go away
        if (OperatingSystem.IsWindows())
        {
            var delta = new MSDelta.MSDelta();
            Creator = delta;
            Applier = delta;
        }
    }

    protected override void CheckDeltaFile(Stream targetFileStream, Stream expectedTargetFileStream)
    {
        var targetHash = GetDeltaHash(targetFileStream);
        var expectedTargetHash = GetDeltaHash(expectedTargetFileStream);
        Assert.That(targetHash, Is.EqualTo(expectedTargetHash));
    }

    private static unsafe string? GetDeltaHash(Stream deltaStream)
    {
        //We don't actually need this but makes warnings go away
        if (OperatingSystem.IsWindows())
        {
            var deltaBytes = new byte[deltaStream.Length];
            deltaStream.ReadExactly(deltaBytes, 0, deltaBytes.Length);
        
            fixed (byte* deltaBuf = deltaBytes)
            {
                var deltaDeltaInput = new DeltaInput(deltaBuf, deltaBytes.Length, true);
                if (MSDelta.MSDelta.GetDeltaInfoB(deltaDeltaInput, out var info))
                {
                    return info.TargetHash.GetHash();
                }
            }
        }
        
        return null;
    }
}