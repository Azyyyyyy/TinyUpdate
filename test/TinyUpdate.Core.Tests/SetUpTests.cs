using NUnit.Framework;
using TinyUpdate.Core.Logging;
using TinyUpdate.Test;

namespace TinyUpdate.Core.Tests
{
    [SetUpFixture]
    public class SetUpTests
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            LoggingCreator.AddLogBuilder(new TestLoggerBuilder());
        }
    }
}