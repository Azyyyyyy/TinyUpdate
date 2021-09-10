using System;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Core.Tests.Logging
{
    internal class DisposableDummyLogger : ILogging, IDisposable
    {
        public DisposableDummyLogger(string name)
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

        public void Dispose()
        {
        }
    }
    
    internal class DisposableDummyLoggerBuilder : LoggingBuilder
    {
        public override ILogging CreateLogger(string name) => new DummyLogger(name);
    }
}