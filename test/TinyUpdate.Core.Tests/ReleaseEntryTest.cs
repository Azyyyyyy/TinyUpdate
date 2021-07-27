using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;
using TinyUpdate.Core.Exceptions;
using TinyUpdate.Core.Utils;
using SemVersion;

namespace TinyUpdate.Core.Tests
{
    [Ignore("Needs remaking")]
    public class ReleaseEntryTest
    {
        private async Task RunIsValidReleaseEntryTest(InvalidReleaseEntry invalidReleaseEntry, bool createFile, bool onlyCheckFail = false)
        {
            var releaseFile = await DummyReleaseEntry.MakeDummyReleaseEntry(createFile, ".tuup");
            Assert.IsTrue(releaseFile.IsValidReleaseEntry(SemanticVersion.BaseVersion(), createFile), 
                "ReleaseEntry checking failed when we should of passed");

            if (!onlyCheckFail)
            {
                releaseFile = await DummyReleaseEntry.MakeDummyReleaseEntry(createFile, ".tuup", invalidReleaseOptions: invalidReleaseEntry);
                Assert.IsFalse(releaseFile.IsValidReleaseEntry(SemanticVersion.BaseVersion(), createFile), 
                    "ReleaseEntry checking passed when we should of failed");
            }
        }
        
        [Test]
        public async Task IsValidReleaseEntry_ApplicationVersionCheck()
        {
            await RunIsValidReleaseEntryTest(InvalidReleaseEntry.Version, false);
        }
        
        [Test]
        public async Task IsValidReleaseEntry_FilesizeCheck()
        {
            Assert.ThrowsAsync<Exception>(async () =>
                await DummyReleaseEntry.MakeDummyReleaseEntry(false, "",
                    invalidReleaseOptions: InvalidReleaseEntry.Filesize));

            await RunIsValidReleaseEntryTest(InvalidReleaseEntry.Filesize, true, true);
        }
        
        [Test]
        public async Task IsValidReleaseEntry_SHA256Check()
        {
            Assert.ThrowsAsync<Exception>(async () => await RunIsValidReleaseEntryTest(InvalidReleaseEntry.SHA256, true));

            await RunIsValidReleaseEntryTest(InvalidReleaseEntry.Data, true);
        }

        [Test]
        public void IsValidReleaseEntry_OldVersionCheck()
        {
            var data = new byte[69];
            Randomizer.NextBytes(data);
            var hash = SHA256Util.CreateSHA256Hash(data);
            var version = new SemanticVersion(1, 2, 0);
            Assert.DoesNotThrow(() => 
                new ReleaseEntry(hash, "wew", data.Length, false, version, ""));
            
            Assert.Throws<Exception>(() => 
                new ReleaseEntry(hash, "wew", data.Length, true, version, ""));

            Assert.DoesNotThrow(() => 
                new ReleaseEntry(hash, "wew", data.Length, true, version, "", oldVersion: new SemanticVersion(1, 1, 0)));
        }
        
        [Test]
        public async Task IsValidReleaseEntry_FileCheck()
        {
            //Check that we fail the file when the file doesn't exist
            var releaseFile = await DummyReleaseEntry.MakeDummyReleaseEntry(false, ".tuup");
            Assert.IsFalse(releaseFile.IsValidReleaseEntry(new SemanticVersion(1, 0, 0), true), "File checking failed, Returning true when we should have false");
            
            //Check that we check the file correctly when everything is passed as it should be
            releaseFile = await DummyReleaseEntry.MakeDummyReleaseEntry(true, ".tuup");
            Assert.IsTrue(releaseFile.IsValidReleaseEntry(new SemanticVersion(1, 0, 0), true), "File checking failed, Returning false when we should have true");
            
            //Now we make one with a invalid filename name (no filename + invalid name), we should throw when this is the case
            Assert.ThrowsAsync<InvalidFileNameException>(async () => await DummyReleaseEntry.MakeDummyReleaseEntry(false, "", ""), "We didn't throw on a filename that is nothing!");
            Assert.ThrowsAsync<InvalidFileNameException>(async () => await DummyReleaseEntry.MakeDummyReleaseEntry(false, CreateInvalidFilename()), "We didn't throw on a filename that is invalid!");
        }

        private static Randomizer Randomizer => TestContext.CurrentContext.Random;
        
        /// <summary>
        /// Gets a random <see cref="char"/> from a <see cref="char"/>[]
        /// </summary>
        /// <param name="chars"><see cref="char"/>[] to grab a random <see cref="char"/> from</param>
        private static char GetRandomChar(IReadOnlyList<char> chars) => chars[Randomizer.Next(0, chars.Count - 1)];
        
        /// <summary>
        /// Creates a invalid file name
        /// </summary>
        private static string CreateInvalidFilename() => 
            Path.GetRandomFileName() + GetRandomChar(Path.GetInvalidPathChars()) + Path.GetRandomFileName();
    }
}