using NUnit.Framework;
using TinyUpdate.Core.Helper;
using System.Runtime.InteropServices;

namespace TinyUpdate.Core.Tests.Helper
{
    public class OsHelperTest
    {
#if !Windows && !Linux && !macOS
#else
        [Test]
        public void IsCorrectOS()
        {
            var os =
#if Windows
                OSPlatform.Windows;
#elif Linux
                OSPlatform.Linux;
#elif macOS
                OSPlatform.OSX;
#endif
            Assert.IsTrue(OSHelper.ActiveOS == os, "We should be reporting '{0}' as the active OS but reported '{1}'", os, OSHelper.ActiveOS);
        }
#endif
    }
}