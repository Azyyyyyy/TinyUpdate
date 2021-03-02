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
        public async Task ApplyUpdate_ReleaseEntry()
        {
            Global.ApplicationFolder = @"C:\Users\aaron\AppData\Local\osulazer";
            Global.ApplicationVersion = Version.Parse("2021.129.0");
            
            //Get details about update file
            var releaseFileLocation = @"C:\Users\aaron\AppData\Local\Temp\TinyUpdate\TestRunner\r0upvlv4.pb3.tuup";
            var fileStream = File.OpenRead(releaseFileLocation);
            var fileHash = SHA1Util.CreateSHA1Hash(fileStream);
            var fileLength = fileStream.Length;
            fileStream.Dispose();

            var deltaFileProgressStream = File.OpenWrite("apply_delta.txt");
            var deltaFileProgressStreamText = new StreamWriter(deltaFileProgressStream);

            var res = new ReleaseEntry(
                fileHash, 
                Path.GetFileName(releaseFileLocation),
                fileLength,
                true, 
                Version.Parse("2021.129.1"),
                @"C:\Users\aaron\AppData\Local\Temp\TinyUpdate\TestRunner");
            await _updateApplier.ApplyUpdate(res, obj => deltaFileProgressStreamText.WriteLine($"Progress: {obj * 100}"));
            deltaFileProgressStreamText.Dispose();
            deltaFileProgressStream.Dispose();
        }

        [Test]
        public Task ApplyUpdate_UpdateInfo()
        {
            //UpdateInfo updateInfo, Action<decimal>? progress
            throw new NotImplementedException();
        }
    }
}