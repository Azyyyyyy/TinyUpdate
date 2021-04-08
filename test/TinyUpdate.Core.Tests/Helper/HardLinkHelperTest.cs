using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NUnit.Framework;
using TinyUpdate.Core.Helper;

namespace TinyUpdate.Core.Tests.Helper
{
    public class HardLinkHelperTest
    {
        [Test]
        public async Task HardLink()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Assert.Inconclusive("Testing running on macOS, can't run hard link test due to no Hard Link support on macOS...");
            }
            
            //Get some random text (using this works)
            var randomText = Path.GetRandomFileName();
            
            // Make a random file and fill it with some content
            var baseFile = Path.GetRandomFileName();
            var baseFileStream = File.CreateText(baseFile);
            await baseFileStream.WriteAsync(randomText);
            await baseFileStream.DisposeAsync();
            
            //Make up another random filename and make a hard link to the first random filename
            var randomFile = Path.GetRandomFileName();
            Assert.True(HardLinkHelper.CreateHardLink(baseFile, randomFile), "Wasn't able to create hard link");
            
            //Check that the file does now exist on disk
            Assert.True(File.Exists(randomFile), "Hard link was created but file doesn't exist...");

            //Check that both random files contains the same content 
            var randomFileContent = await File.ReadAllTextAsync(randomFile);
            Assert.True(randomFileContent == randomText, $"Content in file {randomFile} should be '{randomText}' but was '{randomFileContent}'");
            
            //Check that both random files contains the same content after changing the contents of both, starting with the random file
            var moreRandomText = Path.GetRandomFileName();
            randomText += moreRandomText;
            await File.AppendAllTextAsync(randomFile, moreRandomText);
            randomFileContent = await File.ReadAllTextAsync(baseFile);
            Assert.True(randomFileContent == randomText, $"Content in file {baseFile} should be '{randomText}' but was '{randomFileContent}'");
            
            //Now the base file
            moreRandomText = Path.GetRandomFileName();
            randomText += moreRandomText;
            await File.AppendAllTextAsync(baseFile, moreRandomText);
            randomFileContent = await File.ReadAllTextAsync(randomFile);
            Assert.True(randomFileContent == randomText, $"Content in file {randomFile} should be '{randomText}' but was '{randomFileContent}'");
        }
    }
}