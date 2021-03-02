using NUnit.Framework;
using TinyUpdate.Test;

namespace TinyUpdate.Core.Tests
{
    [SetUpFixture]
    public class SetUpTests
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            SetupTests.OneTimeSetUp();
        }

        [OneTimeTearDown]
        public void RunAfterAnyTests()
        {
            SetupTests.OneTimeTearDown();
        }
    }
}