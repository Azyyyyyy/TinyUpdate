using System.IO.Compression;
using Microsoft.Extensions.Logging.Abstractions;
using TinyUpdate.Core;
using TinyUpdate.Packages.Tests.Abstract;
using TinyUpdate.TUUP;

namespace TinyUpdate.Packages.Tests;

public class TuupUpdatePackageTests : UpdatePackageCan
{
    private readonly SHA256 _sha256 = SHA256.Instance;
    
    [SetUp]
    public void Setup()
    {
        var mockApplier1 = CreateMockDeltaApplier(".bsdiff");
        var mockApplier2 = CreateMockDeltaApplier(".diffing");

        var mockCreation1 = CreateMockDeltaCreation(".bsdiff", NeedsFixedCreatorSize ? 0.5 : null);
        var mockCreation2 = CreateMockDeltaCreation(".diffing", NeedsFixedCreatorSize ? 0.3 : null);
        
        var deltaManager = new DeltaManager(
            [ mockApplier1.Object, mockApplier2.Object ],
            [ mockCreation1.Object, mockCreation2.Object ]);

        UpdatePackage = new TuupUpdatePackage(deltaManager, _sha256);
        UpdatePackageCreator = new TuupUpdatePackageCreator(_sha256, deltaManager, FileSystem, new TuupUpdatePackageCreatorOptions());
    }

    private static bool NeedsFixedCreatorSize
    {
        get
        {
            var args = TestContext.CurrentContext.Test.Arguments;
            return args.Length != 0 && args[0] is DeltaUpdatePackageTestData
            {
                NeedsFixedCreatorSize: true
            };
        }
    }

    protected override void CheckUpdatePackageWithExpected(Stream targetFileStream, Stream expectedTargetFileStream)
    {
        var targetFileStreamZip = new ZipArchive(targetFileStream, ZipArchiveMode.Read);
        var expectedTargetFileStreamZip = new ZipArchive(expectedTargetFileStream, ZipArchiveMode.Read);
        
        Assert.Multiple(() =>
        {
            Assert.That(expectedTargetFileStreamZip.Entries, 
                Has.Count.EqualTo(targetFileStreamZip.Entries.Count), 
                () => "They isn't the correct amount of files");

            Assert.That(expectedTargetFileStreamZip.Entries.Select(x => x.FullName).OrderDescending(), 
                Is.EquivalentTo(targetFileStreamZip.Entries.Select(x => x.FullName).OrderDescending()), 
                () => "File structure is not the same in both files");

            foreach (var expectedEntry in expectedTargetFileStreamZip.Entries)
            {
                var targetEntry = targetFileStreamZip.GetEntry(expectedEntry.FullName);
                if (targetEntry == null)
                {
                    Assert.Fail($"{expectedEntry.FullName} doesn't exist within the target file");
                    continue;
                }

                using var expectedTargetEntryStream = targetEntry.Open();
                using var targetEntryStream = targetEntry.Open();

                using var expectedTargetEntryMemoryStream = new MemoryStream();
                using var targetEntryMemoryStream = new MemoryStream();

                expectedTargetEntryStream.CopyTo(expectedTargetEntryMemoryStream);
                targetEntryStream.CopyTo(targetEntryMemoryStream);
                
                Assert.That(expectedTargetEntryMemoryStream.Length, Is.EqualTo(targetEntryMemoryStream.Length), () => "They is a filesize difference");

                var expectedTargetEntryHash = _sha256.CreateSHA256Hash(expectedTargetEntryMemoryStream);
                var targetEntryHash = _sha256.CreateSHA256Hash(targetEntryMemoryStream);
                
                Assert.That(expectedTargetEntryHash, Is.EqualTo(targetEntryHash), () => "File contents are different");
            }
        });
    }
}