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
            var releaseFileLocation = @"E:\aaron\Downloads\ryqs43xb.uzg.tuup";
            var fileStream = File.OpenRead(releaseFileLocation);
            var fileHash = SHA1Util.CreateSHA1Hash(fileStream);
            var fileLength = fileStream.Length;
            fileStream.Dispose();

            var res = new ReleaseEntry(
                fileHash, 
                Path.GetFileName(releaseFileLocation),
                fileLength,
                true, 
                Version.Parse("2021.129.1"),
                @"E:\aaron\Downloads");
            await _updateApplier.ApplyUpdate(res);
        }

        [Test]
        public Task ApplyUpdate_UpdateInfo()
        {
            //UpdateInfo updateInfo, Action<decimal>? progress
            throw new NotImplementedException();
        }
    }
}