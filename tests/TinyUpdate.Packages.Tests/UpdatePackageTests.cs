﻿using System.IO.Compression;
using Microsoft.Extensions.Logging.Abstractions;
using TinyUpdate.Core;
using TinyUpdate.Core.Tests.Attributes;
using TinyUpdate.Packages.Tests.Abstract;
using TinyUpdate.Packages.Tests.Model;
using TinyUpdate.Packages.Tests.TestSources;
using TinyUpdate.TUUP;

namespace TinyUpdate.Packages.Tests;

[DynamicTestCaseSource(nameof(TestFullPackageCreation), typeof(TuupUpdatePackageTestSource), nameof(TuupUpdatePackageTestSource.GetFullTests))]
[DynamicTestCaseSource(nameof(TestDeltaPackageCreation), typeof(TuupUpdatePackageTestSource), nameof(TuupUpdatePackageTestSource.GetDeltaTests))]
public class TuupUpdatePackageTests : UpdatePackageCan
{
    private readonly SHA256 _sha256Hasher = SHA256.Instance;
    
    [SetUp]
    public void Setup()
    {
        var mockApplier1 = CreateMockDeltaApplier(".bsdiff");
        var mockApplier2 = CreateMockDeltaApplier(".diffing");

        var mockCreation1 = CreateMockDeltaCreation(".bsdiff", NeedsFixedCreatorSize ? 0.5 : null);
        var mockCreation2 = CreateMockDeltaCreation(".diffing", NeedsFixedCreatorSize ? 0.3 : null);
        
        var deltaManager = new DeltaManager(
            [mockApplier1.Object, mockApplier2.Object],
            [mockCreation1.Object, mockCreation2.Object],
            NullLogger.Instance);

        var tuupPackageCreator = new TuupUpdatePackageCreator(_sha256Hasher, deltaManager, FileSystem,
            new TuupUpdatePackageCreatorOptions());
 
        UpdatePackage = new TuupUpdatePackage(deltaManager, _sha256Hasher);
        DeltaPackageCreator = tuupPackageCreator;
        FullPackageCreator = tuupPackageCreator;
    }

    private static bool NeedsFixedCreatorSize
    {
        get
        {
            var args = TestContext.CurrentContext.Test.Arguments;
            return args.Length != 0 && args[0] is DeltaUpdatePackageTestData { NeedsFixedCreatorSize: true };
        }
    }

    protected override void CheckUpdatePackageWithExpected(Stream targetStream, Stream expectedTargetStream)
    {
        var targetStreamZip = new ZipArchive(targetStream, ZipArchiveMode.Read);
        var expectedTargetStreamZip = new ZipArchive(expectedTargetStream, ZipArchiveMode.Read);
        
        Assert.Multiple(() =>
        {
            Assert.That(expectedTargetStreamZip.Entries, 
                Has.Count.EqualTo(targetStreamZip.Entries.Count), 
                () => "They isn't the correct amount of files");

            Assert.That(expectedTargetStreamZip.Entries.Select(x => x.FullName).OrderDescending(), 
                Is.EquivalentTo(targetStreamZip.Entries.Select(x => x.FullName).OrderDescending()), 
                () => "File structure is not the same in both files");

            foreach (var expectedEntry in expectedTargetStreamZip.Entries)
            {
                var targetEntry = targetStreamZip.GetEntry(expectedEntry.FullName);
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

                var expectedTargetEntryHash = _sha256Hasher.HashData(expectedTargetEntryMemoryStream);
                var targetEntryHash = _sha256Hasher.HashData(targetEntryMemoryStream);
                
                Assert.That(expectedTargetEntryHash, Is.EqualTo(targetEntryHash), () => "File contents are different");
            }
        });
    }
}