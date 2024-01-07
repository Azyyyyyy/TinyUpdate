﻿using System.IO.Compression;
using Microsoft.Extensions.Logging.Abstractions;
using TinyUpdate.Core;
using TinyUpdate.Packages.Tests.Abstract;
using TinyUpdate.TUUP;

namespace TinyUpdate.Packages.Tests;

public class TuupUpdatePackageTests : UpdatePackageCan
{
    private SHA256 SHA256 = new SHA256(NullLogger.Instance);
    
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

        UpdatePackage = new TuupUpdatePackage(deltaManager, SHA256);
        UpdatePackageCreator = new TuupUpdatePackageCreator(SHA256, deltaManager, FileSystem, new TuupUpdatePackageCreatorOptions());
    }

    protected static bool NeedsFixedCreatorSize => TestContext.CurrentContext.Test.Arguments[0] is DeltaUpdatePackageTestData
    {
        NeedsFixedCreatorSize: true
    };

    protected override void CheckUpdatePackageWithExpected(Stream targetFileStream, Stream expectedTargetFileStream)
    {
        var targetFileStreamZip = new ZipArchive(targetFileStream, ZipArchiveMode.Read);
        var expectedTargetFileStreamZip = new ZipArchive(expectedTargetFileStream, ZipArchiveMode.Read);
        
        Assert.Multiple(() =>
        {
            Assert.That(expectedTargetFileStreamZip.Entries, 
                Has.Count.EqualTo(targetFileStreamZip.Entries.Count), 
                () => "They isn't the correct amount of files");

            Assert.That(expectedTargetFileStreamZip.Entries.Select(x => x.Name).OrderDescending(), 
                Is.EquivalentTo(targetFileStreamZip.Entries.Select(x => x.Name).OrderDescending()), 
                () => "File structure is not the same in both files");

            foreach (var expectedEntry in expectedTargetFileStreamZip.Entries)
            {
                var targetEntry = targetFileStreamZip.GetEntry(expectedEntry.FullName);
                if (targetEntry == null)
                {
                    Assert.Fail($"{expectedEntry.Name} doesn't exist within the target file");
                    continue;
                }

                using var expectedTargetEntryStream = targetEntry.Open();
                using var targetEntryStream = targetEntry.Open();

                using var expectedTargetEntryMemoryStream = new MemoryStream();
                using var targetEntryMemoryStream = new MemoryStream();

                expectedTargetEntryStream.CopyTo(expectedTargetEntryMemoryStream);
                targetEntryStream.CopyTo(targetEntryMemoryStream);
                
                Assert.That(expectedTargetEntryMemoryStream.Length, Is.EqualTo(targetEntryMemoryStream.Length), () => "They is a filesize difference");

                var expectedTargetEntryHash = SHA256.CreateSHA256Hash(expectedTargetEntryMemoryStream);
                var targetEntryHash = SHA256.CreateSHA256Hash(targetEntryMemoryStream);
                
                Assert.That(expectedTargetEntryHash, Is.EqualTo(targetEntryHash), () => "File contents are different");
            }
        });
    }
}