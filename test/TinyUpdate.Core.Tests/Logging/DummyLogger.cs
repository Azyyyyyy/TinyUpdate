using System;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Core.Tests.Logging
{
    internal class DummyLogger : ILogging
    {
        public DummyLogger(string name)
        {
            Name = name;
        }
        
        public string Name { get; }
        public LogLevel? LogLevel { get; set; }
        public void Debug(string message, params object?[] propertyValues)
        {
            throw new NotImplementedException();
        }

        public void Information(string message, params object?[] propertyValues)
        {
            throw new NotImplementedException();
        }

        public void Warning(string message, params object?[] propertyValues)
        {
            throw new NotImplementedException();
        }

        public void Error(string message, params object?[] propertyValues)
        {
            throw new NotImplementedException();
        }

        public void Error(Exception e, params object?[] propertyValues)
        {
            throw new NotImplementedException();
        }
    }
    
    internal class DummyLoggerBuilder : LoggingBuilder
    {
        public override ILogging CreateLogger(string name) => new DummyLogger(name);
    }
}