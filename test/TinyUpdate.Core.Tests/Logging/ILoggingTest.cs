﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Core.Tests.Logging
{
    [NonParallelizable]
    // ReSharper disable once InconsistentNaming
    public abstract class ILoggingTest<TLogger, TBuilder> 
        where TLogger : ILogging
        where TBuilder : LoggingBuilder
    {
        protected TLogger Logger;
        private readonly TBuilder _builder;
        private readonly bool _isDisposable;
        protected ILoggingTest(TBuilder builder)
        {
            _builder = builder;
            Logger = (TLogger)builder.CreateLogger("Test-Log");
            _isDisposable = typeof(TLogger).GetInterface(nameof(IDisposable)) == null;
        }

        [Test]
        public async Task LogCorrectlyOnGlobalTrace()
        {
            Logger.LogLevel = null;
            LoggingCreator.GlobalLevel = LogLevel.Trace;
            await TestLogging_Output(() => Logger.Debug("Test"));
            await TestLogging_Output(() => Logger.Information("Test"));
            await TestLogging_Output(() => Logger.Warning("Test"));
            await TestLogging_Output(() => Logger.Error("Test"));
            await TestLogging_Output(() => Logger.Error(new Exception()));
        }

        [Test]
        public async Task LogCorrectlyOnGlobalInfo()
        {
            Logger.LogLevel = null;
            LoggingCreator.GlobalLevel = LogLevel.Info;
            await TestLogging_NoOutput(() => Logger.Debug("Test"));
            await TestLogging_Output(() => Logger.Information("Test"));
            await TestLogging_Output(() => Logger.Warning("Test"));
            await TestLogging_Output(() => Logger.Error("Test"));
            await TestLogging_Output(() => Logger.Error(new Exception()));
        }

        [Test]
        public async Task LogCorrectlyOnGlobalWarn()
        {
            Logger.LogLevel = null;
            LoggingCreator.GlobalLevel = LogLevel.Warn;
            await TestLogging_NoOutput(() => Logger.Debug("Test"));
            await TestLogging_NoOutput(() => Logger.Information("Test"));
            await TestLogging_Output(() => Logger.Warning("Test"));
            await TestLogging_Output(() => Logger.Error("Test"));
            await TestLogging_Output(() => Logger.Error(new Exception()));
        }
        
        [Test]
        public async Task LogCorrectlyOnGlobalError()
        {
            Logger.LogLevel = null;
            LoggingCreator.GlobalLevel = LogLevel.Error;
            await TestLogging_NoOutput(() => Logger.Debug("Test"));
            await TestLogging_NoOutput(() => Logger.Information("Test"));
            await TestLogging_NoOutput(() => Logger.Warning("Test"));
            await TestLogging_Output(() => Logger.Error("Test"));
            await TestLogging_Output(() => Logger.Error(new Exception()));
        }

        //NOTE: We do 'LoggingCreator.GlobalLevel = LogLevel.Error;' here because it should ignore the GlobalLevel and go by it's log level

        [Test]
        public async Task LogCorrectlyOnTrace()
        {
            Logger.LogLevel = LogLevel.Trace;
            LoggingCreator.GlobalLevel = LogLevel.Error;
            await TestLogging_Output(() => Logger.Debug("Test"));
            await TestLogging_Output(() => Logger.Information("Test"));
            await TestLogging_Output(() => Logger.Warning("Test"));
            await TestLogging_Output(() => Logger.Error("Test"));
            await TestLogging_Output(() => Logger.Error(new Exception()));
        }

        [Test]
        public async Task LogCorrectlyOnInfo()
        {
            Logger.LogLevel = LogLevel.Info;
            LoggingCreator.GlobalLevel = LogLevel.Error;
            await TestLogging_NoOutput(() => Logger.Debug("Test"));
            await TestLogging_Output(() => Logger.Information("Test"));
            await TestLogging_Output(() => Logger.Warning("Test"));
            await TestLogging_Output(() => Logger.Error("Test"));
            await TestLogging_Output(() => Logger.Error(new Exception()));
        }

        [Test]
        public async Task LogCorrectlyOnWarn()
        {
            Logger.LogLevel = LogLevel.Warn;
            LoggingCreator.GlobalLevel = LogLevel.Error;
            await TestLogging_NoOutput(() => Logger.Debug("Test"));
            await TestLogging_NoOutput(() => Logger.Information("Test"));
            await TestLogging_Output(() => Logger.Warning("Test"));
            await TestLogging_Output(() => Logger.Error("Test"));
            await TestLogging_Output(() => Logger.Error(new Exception()));
        }
        
        [Test]
        public async Task LogCorrectlyOnError()
        {
            Logger.LogLevel = LogLevel.Error;
            LoggingCreator.GlobalLevel = LogLevel.Error;
            await TestLogging_NoOutput(() => Logger.Debug("Test"));
            await TestLogging_NoOutput(() => Logger.Information("Test"));
            await TestLogging_NoOutput(() => Logger.Warning("Test"));
            await TestLogging_Output(() => Logger.Error("Test"));
            await TestLogging_Output(() => Logger.Error(new Exception()));
        }

        [Test]
        public void CreateMultipleLoggers()
        {
            var redoTest = true;
            LoggingCreator.GlobalLevel = LogLevel.Trace;
            while (true)
            {
                var logger1 = MakeLogger();
                var logger2 = MakeLogger();
                var logger3 = MakeLogger();

                logger1.Debug("Testing");
                logger2.Debug("Testing");
                logger3.Debug("Testing");

                //Also disposable the loggers if they exist,
                //we might crash when we try to cleanup
                if (logger1 is IDisposable logger1D)
                {
                    logger1D.Dispose();
                    ((IDisposable)logger2).Dispose();
                    ((IDisposable)logger3).Dispose();
                }

                //and try the test again, we might crash if
                //we remake the loggers
                if (redoTest)
                {
                    redoTest = false;
                    continue;
                }
                break;
            }
        }

        private TLogger MakeLogger(string name = "Test")
        {
            var logger = (TLogger)_builder.CreateLogger(name);
            logger.LogLevel = null;
            return logger;
        }

        private void CheckIDisposable()
        {
            if (_isDisposable)
            {
                Assert.Ignore("{0} doesn't have IDisposable, can't run this test", _loggerName);
            }
        }
        
        [Test]
        public void CreateAndDisposeMultipleLogs()
        {
            CheckIDisposable();
            
            LoggingCreator.GlobalLevel = LogLevel.Trace;
            var logger1 = MakeLogger();
            var logger2 = MakeLogger();
            var logger3 = MakeLogger();
            
            logger1.Debug("Test123");
            logger2.Debug("Test123");
            logger3.Debug("Test123");
            ((IDisposable)logger3).Dispose();
            
            logger1.Debug("Test12");
            logger2.Debug("Test12");
            ((IDisposable)logger2).Dispose();
            
            logger1.Debug("Test12");
            ((IDisposable)logger1).Dispose();
        }

        //TODO: See why this IS working on FileLogger
        [Test]
        [SuppressMessage("ReSharper", "TooWideLocalVariableScope")]
        public void DisposeAndRemake()
        {
            CheckIDisposable();

            LoggingCreator.GlobalLevel = LogLevel.Trace;
            var firstDispose = true;
            TLogger logger1;
            TLogger logger2;
            TLogger logger3;
            while (true)
            {
                logger1 = MakeLogger("Test2");
                logger2 = MakeLogger("Test2");
                logger3 = MakeLogger("Test2");
                
                logger1.Debug("Test123");
                logger2.Debug("Test123");
                logger3.Debug("Test123");
                ((IDisposable)logger1).Dispose();
                ((IDisposable)logger2).Dispose();
                ((IDisposable)logger3).Dispose();

                if (!firstDispose)
                {
                    break;
                }
                firstDispose = false;
            }
        }
        
        //We want to test this first as we need to look
        //at the whole output and run it a load of times
        //so any sign of the logger not being thread safe happens
        private int _outputCount;
        [Test]
        [Order(0)]
        [Repeat(100)]
        public void IsThreadSafe()
        {
            LoggingCreator.GlobalLevel = LogLevel.Trace;
            Logger.LogLevel = null;

            //We also want to spin a load of threads for the same reason
            var threads = new Thread[20];
            var threadsSent = new List<int>(threads.Length);
            for (var i = 0; i < threads.LongLength; i++)
            {
                threads[i] = new Thread(p => Logger.Debug("Wew " + (int?)p));
                threadsSent.Add(i);
            }
            for (var i = 0; i < threads.LongLength; i++)
            {
                threads[i].Start(i);
            }
            for (var i = 0; i < threads.LongLength; i++)
            {
                threads[i].Join();
            }

            var output = GetWholeOutput()[_outputCount..];
            _outputCount += output.Length;
            var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            if (lines.LongLength != threads.LongLength)
            {
                Assert.Fail("There's too much/little amount of output (Or it incorrectly doing spacing in the wrong place)" + Environment.NewLine + output);
            }

            for (var i = 0; i < threads.LongLength; i++)
            {
                // ReSharper disable once AccessToModifiedClosure
                var index = lines.IndexOf(x => x!.Contains("Wew " + i));
                if (index != -1)
                {
                    threadsSent.Remove(i);
                }
            }

            if (threadsSent.Any())
            {
                Assert.Fail("The logger didn't get anything from {0} threads!. What we got{1}", threadsSent.Count, Environment.NewLine + output);
            }
        }

        //This will also indirectly test if we don't crash when doing this
        [Test]
        public async Task PropertiesFormatCorrectly()
        {
            LoggingCreator.GlobalLevel = LogLevel.Trace;
            Logger.LogLevel = null;
            
            const string testMessage = "This is a {0} which should show 'true' in the brackets ({1}) and was made by {2}";
            var objects = new object[] { "message", true, _loggerName };
            var expectedResult = $"This is a message which should show 'true' in the brackets (True) and was made by {_loggerName}";

            /*On these checks we do Contains as the logger might add it's own content
              which is fine, we just want to make sure the message part is done correctly*/
            var debugMessage = await WaitForLog(() => Logger.Debug(testMessage, objects));
            Assert.True(debugMessage.Contains(expectedResult), "We didn't get the expected result but {0}", debugMessage);
            
            var infoMessage = await WaitForLog(() => Logger.Information(testMessage, objects));
            Assert.True(infoMessage.Contains(expectedResult), "We didn't get the expected result but {0}", infoMessage);
            
            var warnMessage = await WaitForLog(() => Logger.Warning(testMessage, objects));
            Assert.True(warnMessage.Contains(expectedResult), "We didn't get the expected result but {0}", warnMessage);
            
            var errorMessage = await WaitForLog(() => Logger.Error(testMessage, objects));
            Assert.True(errorMessage.Contains(expectedResult), "We didn't get the expected result but {0}", errorMessage);

            //What comes from the exception overload in the logger can be different per logger.
            //Give GetExceptionMessage what we are about to use so it can tell us how it should be
            //when the logger actually outputs
            var testException = new Exception("This is an exception!");
            var expectedExceptionResult = GetExceptionMessage(testException, objects);
            var exceptionErrorMessage = await WaitForLog(() => Logger.Error(testException, objects));
            Assert.True(exceptionErrorMessage.Contains(expectedExceptionResult), "We didn't get the expected result but {0}", exceptionErrorMessage);
        }

        protected abstract string GetExceptionMessage(Exception e, object?[] props);

        protected abstract string GetWholeOutput();

        protected abstract Task<string> WaitForLog(Action? action = null);

        protected abstract Task<bool> DoesLogOutput(Action action);

        private readonly string _loggerName = typeof(TLogger).FullName!;
        private async Task TestLogging_Output(Action action)
        {
            Assert.True(
                await DoesLogOutput(action), "{0} should of outputted but it didn't (Global Level: {1}, Logger Level: {2})", _loggerName, LoggingCreator.GlobalLevel, Logger.LogLevel);
        }

        private async Task TestLogging_NoOutput(Action action)
        {
            Assert.False(
                await DoesLogOutput(action), "{0} shouldn't of outputted but it did (Global Level: {1}, Logger Level: {2})", _loggerName, LoggingCreator.GlobalLevel, Logger.LogLevel);
        }
    }
}