using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using TinyUpdate.Core;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Test.Update
{
    public class IUpdateCreatorTest
    {
        private readonly IUpdateCreator _updateCreator;
        public IUpdateCreatorTest(IUpdateCreator updateCreator)
        {
            _updateCreator = updateCreator;
        }
        
        [SetUp]
        public void Setup()
        {
            
        }

        [Test]
        public async Task CreateDeltaPackage()
        {
            var deltaFileProgressStream = File.OpenWrite("create_delta.txt");
            var deltaFileProgressStreamText = new StreamWriter(deltaFileProgressStream);
            
            Global.ApplicationFolder = @"C:\Users\aaron\AppData\Local\osulazer";
            Global.ApplicationVersion = Version.Parse("2021.129.0");
            await _updateCreator.CreateDeltaPackage(
                @"C:\Users\aaron\AppData\Local\osulazer\app-2021.302.0",
                @"C:\Users\aaron\AppData\Local\osulazer\app-2021.129.0", 
                obj => deltaFileProgressStreamText.WriteLine($"Progress: {obj * 100}"));
            deltaFileProgressStreamText.Dispose();
            deltaFileProgressStream.Dispose();
        }
        
        //TODO: Make test for file created (Checking hash and filesize)
        
        [Test]
        [Ignore("Not created yet")]
        public void CreateFullPackage()
        {
        }
    }
}