using System;
using NUnit.Framework;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Logging.Loggers;

namespace TinyUpdate.Core.Tests.Logging
{
    [NonParallelizable]
    public class LoggingCreatorTest
    {
        [Test]
        [Order(0)]
        public void CreateLogger_0LoggersGivesEmptyLogger()
        {
            var logBuilders = LoggingCreator._logBuilders;
            LoggingCreator._logBuilders = Array.Empty<LoggingBuilder>();

            try
            {
                var l = LoggingCreator.CreateLogger(string.Empty);
                Assert.IsInstanceOf<EmptyLogger>(l,
                    "We should of been given an EmptyLogger but got {0}", nameof(l));
            }
            finally
            {
                LoggingCreator._logBuilders = logBuilders;
            }
        }
        
        [Test]
        [Order(1)]
        public void CreateLogger_1LoggerGivesActualLogger()
        {
            var oldLoggers = LoggingCreator._logBuilders;
            LoggingCreator._logBuilders = new LoggingBuilder[] { new DummyLoggerBuilder() };

            try
            {
                var l = LoggingCreator.CreateLogger(string.Empty);
                Assert.IsInstanceOf<DummyLogger>(l,
                    "We should of been given an DummyLogger but got {0}", nameof(l));
            }
            finally
            {
                LoggingCreator._logBuilders = oldLoggers;
            }
        }

        [Test]
        [Order(2)]
        public void CreateLogger_2OrMoreLoggersGivesWrapperLogger()
        {
            var oldLoggers = LoggingCreator._logBuilders;
            LoggingCreator._logBuilders = new LoggingBuilder[]
            {
                new DummyLoggerBuilder(),
                new ConsoleLoggerBuilder()
            };
            var l = LoggingCreator.CreateLogger(string.Empty);

            try
            {
                Assert.IsInstanceOf<WrapperLogger>(l,
                    "We should of been given an WrapperLogger but got {0}", nameof(l));
                ((IDisposable)l).Dispose();
            
                Array.Resize(ref LoggingCreator._logBuilders, 3);
                LoggingCreator._logBuilders[2] = new DisposableDummyLoggerBuilder();
                l = LoggingCreator.CreateLogger(string.Empty);
            
                Assert.IsInstanceOf<WrapperLogger>(l,
                    "We should of been given an WrapperLogger but got {0}", nameof(l));
            }
            finally
            {
                ((IDisposable)l).Dispose();
                LoggingCreator._logBuilders = oldLoggers;
            }
        }

        [Test]
        public void CreateLogger_CanAddLogBuilder()
        {
            var oldLoggers = LoggingCreator._logBuilders;
            try
            {
                LoggingCreator.AddLogBuilder(new DummyLoggerBuilder());
            }
            finally
            {
                LoggingCreator._logBuilders = oldLoggers;
            }
        }

        [Test]
        [TestCase(LogLevel.Trace, null, true, true, true, true)]
        [TestCase(LogLevel.Info, null, false, true, true, true)]
        [TestCase(LogLevel.Warn, null, false, false, true, true)]
        [TestCase(LogLevel.Error, null, false, false, false, true)]
        //We set it as error as it should ignore the global level for the "log" level 
        [TestCase(LogLevel.Error, LogLevel.Trace, true, true, true, true)]
        [TestCase(LogLevel.Error, LogLevel.Info, false, true, true, true)]
        [TestCase(LogLevel.Error, LogLevel.Warn, false, false, true, true)]
        [TestCase(LogLevel.Error, LogLevel.Error, false, false, false, true)]
        public void CreateLogger_ShouldProcessReturnCorrectly(
            LogLevel globalLevel,
            LogLevel? level,
            bool expectedTrace,
            bool expectedInfo,
            bool expectedWarn,
            bool expectedError)
        {
            LoggingCreator.GlobalLevel = globalLevel;
            Assert.True(LoggingCreator.ShouldProcess(level, LogLevel.Trace) == expectedTrace);
            Assert.True(LoggingCreator.ShouldProcess(level, LogLevel.Info) == expectedInfo);
            Assert.True(LoggingCreator.ShouldProcess(level, LogLevel.Warn) == expectedWarn);
            Assert.True(LoggingCreator.ShouldProcess(level, LogLevel.Error) == expectedError);
        }
        
        [Test]
        public void LoggersDisposedAndClearedOnDispose()
        {
            Assert.Ignore();
        }

        [Test]
        public void DoesNotReturnDisposedLogger()
        {
            Assert.Ignore();
        }
    }
}