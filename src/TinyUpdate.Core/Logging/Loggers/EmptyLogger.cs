using System;

namespace TinyUpdate.Core.Logging.Loggers
{
    public class EmptyLogger : ILogging
    {
        private static EmptyLogger? _staticLogger;
        public static EmptyLogger StaticLogger => _staticLogger ??= new EmptyLogger();

        public string Name => string.Empty;
        public LogLevel? LogLevel { get; set; }

        public void Debug(string message, params object?[] propertyValues)
        {
        }

        public void Information(string message, params object?[] propertyValues)
        {
        }

        public void Warning(string message, params object?[] propertyValues)
        {
        }

        public void Error(string message, params object?[] propertyValues)
        {
        }

        public void Error(Exception e, params object?[] propertyValues)
        {
        }
    }
}