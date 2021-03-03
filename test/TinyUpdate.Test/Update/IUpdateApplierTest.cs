using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using TinyUpdate.Core;
using TinyUpdate.Core.Update;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Test.Update
{
    public abstract class IUpdateApplierTest
    {
        private readonly IUpdateApplier _updateApplier;
        protected IUpdateApplierTest(IUpdateApplier updateApplier)
        {
            _updateApplier = updateApplier;
        }
        
        [Test]
        [NonParallelizable]
        public async Task ApplyUpdate_ReleaseEntryDelta()
        {
            Assert.IsTrue(await ApplyUpdate(@"C:\Users\aaron\AppData\Local\Temp\TinyUpdate\TestRunner\cny2x4pl.3eg-bs.tuup"));
        }
        
        [Test]
        [NonParallelizable]
        public async Task ApplyUpdate_ReleaseEntryFull()
        {
            Assert.IsTrue(await ApplyUpdate(@"C:\Users\aaron\AppData\Local\Temp\TinyUpdate\TestRunner\32gxkyyo.dsi-bs.tuup"));
        }

        private async Task<bool> ApplyUpdate(string fileLocation)
        {
            Global.ApplicationFolder = @"C:\Users\aaron\AppData\Local\osulazer";
            Global.ApplicationVersion = Version.Parse("2021.129.0");

            var deltaFileProgressStream = File.OpenWrite("apply_delta.txt");
            var deltaFileProgressStreamText = new StreamWriter(deltaFileProgressStream);

            var res = CreateUpdate(fileLocation);
            var successfulUpdate =
                await _updateApplier.ApplyUpdate(res, obj => deltaFileProgressStreamText.WriteLine($"Progress: {obj * 100}"));
            
            deltaFileProgressStreamText.Dispose();
            deltaFileProgressStream.Dispose();

            return successfulUpdate;
        }

        private static ReleaseEntry CreateUpdate(string fileLocation, Version? version = null, Version? oldVersion = null)
        {
            //Get details about update file
            var releaseFileLocation = fileLocation;
            var fileStream = File.OpenRead(releaseFileLocation);
            var fileHash = SHA1Util.CreateSHA1Hash(fileStream);
            var fileLength = fileStream.Length;
            fileStream.Dispose();

            return new ReleaseEntry(
                fileHash, 
                Path.GetFileName(releaseFileLocation),
                fileLength,
                true, 
                version ?? Version.Parse("2021.129.1"),
                Path.GetDirectoryName(fileLocation),
                oldVersion ?? Version.Parse("2021.129.0"));
        }
        
        [Test]
        [NonParallelizable]
        public async Task ApplyUpdate_UpdateInfo()
        {
            Global.ApplicationFolder = @"C:\Users\aaron\AppData\Local\osulazer";
            Global.ApplicationVersion = Version.Parse("2021.129.0");
            
            var deltaFileProgressStream = File.OpenWrite("apply_delta.txt");
            var deltaFileProgressStreamText = new StreamWriter(deltaFileProgressStream);

            var res = new UpdateInfo(new []
            {
                CreateUpdate(@"C:\Users\aaron\AppData\Local\Temp\TinyUpdate\TestRunner\ao3bgb4d.qoc.tuup", oldVersion: Version.Parse("2021.129.0")),
                CreateUpdate(@"C:\Users\aaron\AppData\Local\Temp\TinyUpdate\TestRunner\uanyv4z0.lrv.tuup", Version.Parse("2021.129.2"), Version.Parse("2021.129.1")),
            });
            var successfulUpdate =
                await _updateApplier.ApplyUpdate(res, obj => deltaFileProgressStreamText.WriteLine($"Progress: {obj * 100}"));
            
            deltaFileProgressStreamText.Dispose();
            deltaFileProgressStream.Dispose();

            Assert.IsTrue(successfulUpdate);
        }
    }
}