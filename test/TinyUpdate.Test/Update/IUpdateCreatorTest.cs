using System;
using System.Threading.Tasks;
using NUnit.Framework;
using TinyUpdate.Core;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Test.Update
{
    public class IUpdateCreatorTest
    {
        private readonly ILogging _logger;
        private readonly IUpdateCreator _updateCreator;
        public IUpdateCreatorTest(IUpdateCreator updateCreator)
        {
            _logger = LoggingCreator.CreateLogger(GetType().Name);
            _updateCreator = updateCreator;
        }
        
        [SetUp]
        public void Setup()
        {
            
        }

        [Test]
        public void CreateDeltaPackage()
        {
            Global.ApplicationFolder = @"C:\Users\aaron\AppData\Local\osulazer";
            Global.ApplicationVersion = Version.Parse("2021.129.0");

            //Apply update
            var wasSuccessful = _updateCreator.CreateDeltaPackage(
                @"C:\Users\aaron\AppData\Local\osulazer\app-2021.302.0",
                new Version("2021.226.0"),
                @"C:\Users\aaron\AppData\Local\osulazer\app-2021.226.0",
                null,
                progress: obj => _logger.Debug($"Progress: {obj * 100}"));
            Assert.True(wasSuccessful, "Wasn't able to apply update");
        }
        
        //TODO: Make test for file created (Checking hash and filesize)
        
        [Test]
        public void CreateFullPackage()
        {
            Global.ApplicationFolder = @"C:\Users\aaron\AppData\Local\osulazer";
            Global.ApplicationVersion = Version.Parse("2021.129.0");
            var wasSuccessful = _updateCreator.CreateFullPackage(
                @"C:\Users\aaron\AppData\Local\osulazer\app-2021.302.0",
                new Version("2021.302.0"),
                null,
                obj => _logger.Debug($"Progress: {obj * 100}"));
            Assert.True(wasSuccessful, "Wasn't able to apply update");
        }
    }
}