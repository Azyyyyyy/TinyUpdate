using System;

namespace TinyUpdate.Core.Logging.Loggers
{
    public class EmptyLogger : ILogging
    {
        public string Name { get; } = "";
        public LogLevel? LogLevel { get; set; }

        public void Debug(string message, params object?[] propertyValues)
        { }

        public void Information(string message, params object?[] propertyValues)
        { }

        public void Warning(string message, params object?[] propertyValues)
        { }

        public void Error(string message, params object?[] propertyValues)
        { }

        public void Error(Exception e, params object?[] propertyValues)
        { }
    }
    
    /// <summary>
    /// Builder to create <see cref="LoggingBuilder"/>
    /// </summary>
    public class EmptyLoggerBuilder : LoggingBuilder
    {
        /// <inheritdoc cref="LoggingBuilder.CreateLogger"/>
        public override ILogging CreateLogger(string name) => new EmptyLogger();
    }
}