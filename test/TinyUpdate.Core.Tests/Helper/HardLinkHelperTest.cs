using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NUnit.Framework;
using TinyUpdate.Core.Helper;

namespace TinyUpdate.Core.Tests.Helper
{
    public class HardLinkHelperTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task HardLink()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Inconclusive("Not running on Windows, can't yet run test on this device");
            }
            
            //Get some random text (using this works)
            var randomText = Path.GetRandomFileName();
            
            // Make a random file and fill it with some content
            var randomFile = Path.GetRandomFileName();
            var randomFileStream = File.CreateText(randomFile);
            await randomFileStream.WriteAsync(randomText);
            await randomFileStream.DisposeAsync();
            
            //Make up another random filename and make a hard link to the first random filename
            var randomFileOther = Path.GetRandomFileName();
            Assert.True(HardLinkHelper.CreateHardLink(randomFile, randomFileOther), "Wasn't able to create hard link");

            //Check that the both random files contains the same content 
            var content = await File.ReadAllTextAsync(randomFileOther);
            Assert.True(content == randomText, $"Content in file {randomFileOther} should be '{randomText}' but was '{content}'");
        }
    }
}