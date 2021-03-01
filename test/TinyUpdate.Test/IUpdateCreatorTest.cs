using System;
using System.Threading.Tasks;
using NUnit.Framework;
using TinyUpdate.Core;

namespace TinyUpdate.Test
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
            Global.ApplicationFolder = @"C:\Users\aaron\AppData\Local\osulazer";
            Global.ApplicationVersion = Version.Parse("2021.129.0");
            await _updateCreator.CreateDeltaPackage(
                @"C:\Users\aaron\AppData\Local\osulazer\app-2021.220.0", 
                @"E:\aaron\Downloads\app-2021.129.0");
        }
        
        //TODO: Make test for file created (Checking hash and filesize)
        
        [Test]
        [Ignore("Not created yet")]
        public void CreateFullPackage()
        {
        }
    }
}