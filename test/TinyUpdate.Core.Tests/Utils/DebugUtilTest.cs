using NUnit.Framework;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Core.Tests.Utils
{
    public class DebugUtilTest
    {
        [Test]
        public void InUnitTest()
        {
            Assert.True(DebugUtil.IsInUnitTest, "We somehow don't know that we are in a unit test!");
        }
    }
}