using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;
using TinyUpdate.Test;

namespace TinyUpdate.Core.Tests
{
    public class ReleaseEntryTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        [Ignore("Not added yet")]
        public async Task IsValidReleaseEntry_ApplicationVersionCheck()
        {
            var releaseFile = await DummyReleaseEntry.MakeDummyReleaseEntry(true);
            Assert.IsTrue(releaseFile.IsValidReleaseEntry(), "File checking failed, making us return false");

            releaseFile = await DummyReleaseEntry.MakeDummyReleaseEntry(true, invalidReleaseOptions: InvalidReleaseEntry.Version);
            Assert.IsFalse(releaseFile.IsValidReleaseEntry(), "File checking failed, making us return true");
        }
        
        [Test]
        [Ignore("Not added yet")]
        public async Task IsValidReleaseEntry_FilesizeCheck()
        {
        }
        
        [Test]
        [Ignore("Not added yet")]
        public async Task IsValidReleaseEntry_IsDeltaCheck()
        {
        }
        
        [Test]
        [Ignore("Not added yet")]
        public async Task IsValidReleaseEntry_SHA1Check()
        {
        }

        [Test]
        public async Task IsValidReleaseEntry_FileCheck()
        {
            //Check that we fail the file when the file doesn't exist
            var releaseFile = await DummyReleaseEntry.MakeDummyReleaseEntry(false);
            Assert.IsFalse(releaseFile.IsValidReleaseEntry(), "File checking failed, Returning true when we should have false");
            
            //Check that we check the file correctly when everything is passed as it should be
            releaseFile = await DummyReleaseEntry.MakeDummyReleaseEntry(true);
            Assert.IsTrue(releaseFile.IsValidReleaseEntry(), "File checking failed, Returning false when we should have true");
            
            //Now we make one with a invalid filename name (no filename + invalid name), we should throw when this is the case
            Assert.ThrowsAsync<Exception>(() => DummyReleaseEntry.MakeDummyReleaseEntry(false, ""), "We didn't throw on a filename that is nothing!");
            Assert.ThrowsAsync<Exception>(() => DummyReleaseEntry.MakeDummyReleaseEntry(false, CreateInvalidFilename()), "We didn't throw on a filename that is invalid!");
        }

        private static readonly Random Rnd = new Randomizer();
        
        /// <summary>
        /// Gets a random <see cref="char"/> from a <see cref="char"/>[]
        /// </summary>
        /// <param name="chars"><see cref="char"/>[] to grab a random <see cref="char"/> from</param>
        private static char GetRandomChar(IReadOnlyList<char> chars) => chars[Rnd.Next(0, chars.Count - 1)];
        
        /// <summary>
        /// Creates a invalid file name
        /// </summary>
        private static string CreateInvalidFilename() => 
            Path.GetRandomFileName() + GetRandomChar(Path.GetInvalidPathChars()) + Path.GetRandomFileName();
    }
}