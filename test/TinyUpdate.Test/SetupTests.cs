using System;
using NUnit.Framework;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Test
{
    /// <summary>
    /// Hooks up anything needed to assist with getting results and logging
    /// </summary>
    public static class SetupTests
    {
        /// <summary>
        /// Setup to call before any tests happen
        /// </summary>
        public static void OneTimeSetUp()
        {
            LoggingCreator.AddLogBuilder(new TestLoggerBuilder());
        }

        /// <summary>
        /// Setup to call after all tests have happened
        /// </summary>
        public static void OneTimeTearDown()
        {
        }
    }
    
    public class TestLogger : ILogging
    {
        public TestLogger(string name)
        {
            Name = name;
        }
        
        public string Name { get; }
        public void Debug(string message, params object[] propertyValues)
        {
            TestContext.WriteLine($"[DEBUG - {Name}]: {message}", propertyValues);
        }

        public void Information(string message, params object[] propertyValues)
        {
            TestContext.WriteLine($"[INFO - {Name}]: {message}", propertyValues);
        }

        public void Warning(string message, params object[] propertyValues)
        {
            TestContext.WriteLine($"[WARNING - {Name}]: {message}", propertyValues);
        }

        public void Error(string message, params object[] propertyValues)
        {
            TestContext.WriteLine($"[ERROR - {Name}]: {message}", propertyValues);
        }

        public void Error(Exception e, params object[] propertyValues)
        {
            Error(e.Message, propertyValues);
        }
    }
    
    public class TestLoggerBuilder : LoggingBuilder
    {
        public override ILogging CreateLogger(string name) => new TestLogger(name);
    }
}