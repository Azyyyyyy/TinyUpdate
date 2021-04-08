using System;
using System.Threading.Tasks;
using NUnit.Framework;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Core.Tests.Update
{
    public class UpdateInfoTest
    {
        [Test]
        public async Task CorrectNewVersion()
        {
            var newestVersion = new Version(1, 3, 1, 1);
            //Give versions in order
            var newestVersionUpdateInfo = new UpdateInfo(new[]
            {
                await DummyReleaseEntry.MakeDummyReleaseEntry(false, ".tps", version: new Version(1, 2)),
                await DummyReleaseEntry.MakeDummyReleaseEntry(false, ".tps", version: new Version(1, 3)),
                await DummyReleaseEntry.MakeDummyReleaseEntry(false, ".tps", version: new Version(1, 3, 1)),
                await DummyReleaseEntry.MakeDummyReleaseEntry(false, ".tps", version: newestVersion),
            }).NewVersion;
            Assert.True(newestVersionUpdateInfo == newestVersion, "We didn't get the newest version, we got version {0} and not version {1}", newestVersionUpdateInfo, newestVersion);

            //Give versions out of order
            newestVersionUpdateInfo = new UpdateInfo(new[]
            {
                await DummyReleaseEntry.MakeDummyReleaseEntry(false, ".tps", version: new Version(1, 2)),
                await DummyReleaseEntry.MakeDummyReleaseEntry(false, ".tps", version: newestVersion),
                await DummyReleaseEntry.MakeDummyReleaseEntry(false, ".tps", version: new Version(1, 3)),
                await DummyReleaseEntry.MakeDummyReleaseEntry(false, ".tps", version: new Version(1, 3, 1)),
            }).NewVersion;
            Assert.True(newestVersionUpdateInfo == newestVersion, "We didn't get the newest version, we got version {0} and not version {1}", newestVersionUpdateInfo, newestVersion);
        }
        
        [Test]
        public async Task CorrectHasUpdate()
        {
            var newestVersionHasUpdate = new UpdateInfo(new[]
            {
                await DummyReleaseEntry.MakeDummyReleaseEntry(false, ".tps", version: new Version(1, 2)),
                await DummyReleaseEntry.MakeDummyReleaseEntry(false, ".tps", version: new Version(1, 3)),
                await DummyReleaseEntry.MakeDummyReleaseEntry(false, ".tps", version: new Version(1, 3, 1)),
            }).HasUpdate;
            Assert.True(newestVersionHasUpdate, "We got reported back that there is no update when that shouldn't be the case");
            
            newestVersionHasUpdate = new UpdateInfo(new[]
            {
                await DummyReleaseEntry.MakeDummyReleaseEntry(false, ".tps", version: new Version(1, 2)),
            }).HasUpdate;
            Assert.True(newestVersionHasUpdate, "We got reported back that there is no update when that shouldn't be the case");
            
            newestVersionHasUpdate = new UpdateInfo(ArraySegment<ReleaseEntry>.Empty).HasUpdate;
            Assert.True(!newestVersionHasUpdate, "We got reported back that there is an update when that shouldn't be the case");
        }
    }
}