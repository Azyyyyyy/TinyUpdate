using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using TinyUpdate.Binary.Entry;

namespace TinyUpdate.Binary.Tests.Delta
{
    public class BSDiffTest
    {
        string outputLocation = "C:\\Users\\aaron\\Downloads\\osu.Game.dll";
        string newFileVersion = "C:\\Users\\aaron\\AppData\\Local\\osulazer\\app-2021.416.0\\osu.Game.dll";
        string oldFileVersion = "C:\\Users\\aaron\\AppData\\Local\\osulazer\\app-2021.331.0\\osu.Game.dll";
        private string updateFile =
            @"C:\Users\aaron\source\repos\TinyUpdate\test\TinyUpdate.Binary.Tests\bin\Debug\net5.0\osu.Game.dll.bsdiff";
        
        [Test]
        public async Task CanCreateUpdateFile()
        {
            //Assert.True(DeltaCreation.CreateBSDiffFile(oldFileVersion, newFileVersion, out var ext, out var deltaFileStream));

            await using var deltaLoc = File.OpenWrite(updateFile);
            //await deltaFileStream.CopyToAsync(deltaLoc);
            //await deltaFileStream.DisposeAsync();
        }

        [Test]
        public async Task CanApplyUpdateFile()
        {
            var fileEntry = new FileEntry(null!, null)
            {
                Stream = File.OpenRead(updateFile)
            };
            //Assert.IsTrue(await DeltaApplying.ApplyBSDiff(fileEntry, outputLocation, oldFileVersion, null));
            await fileEntry.Stream.DisposeAsync();
            
            var createdFile = await File.ReadAllBytesAsync(outputLocation);
            var newFile = await File.ReadAllBytesAsync(newFileVersion);
            Assert.IsTrue(newFile.Length == createdFile.Length);
            for (int i = 0; i < createdFile.Length; i++)
            {
                Assert.IsTrue(createdFile[i] == newFile[i]);
            }
        }
    }
}