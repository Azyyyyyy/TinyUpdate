using System;
using NUnit.Framework;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Test
{
    public class TestLogger : ILogging
    {
        public TestLogger(string name)
        {
            Name = name;
        }
        
        public string Name { get; }
        public LogLevel? LogLevel { get; set; }

        public void Debug(string message, params object[] propertyValues)
        {
            if (LoggingCreator.ShouldProcess(LogLevel, Core.Logging.LogLevel.Trace))
            {
                TestContext.Out.WriteLine($"[DEBUG - {Name}]: {message}", propertyValues);
            }
        }

        public void Information(string message, params object[] propertyValues)
        {
            if (LoggingCreator.ShouldProcess(LogLevel, Core.Logging.LogLevel.Info))
            {
                TestContext.Out.WriteLine($"[INFO - {Name}]: {message}", propertyValues);
            }
        }

        public void Warning(string message, params object[] propertyValues)
        {
            if (LoggingCreator.ShouldProcess(LogLevel, Core.Logging.LogLevel.Warn))
            {
                TestContext.Out.WriteLine($"[WARNING - {Name}]: {message}", propertyValues);
            }
        }

        public void Error(string message, params object[] propertyValues)
        {
            if (LoggingCreator.ShouldProcess(LogLevel, Core.Logging.LogLevel.Error))
            {
                TestContext.Error.WriteLine($"[ERROR - {Name}]: {message}", propertyValues);
            }
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