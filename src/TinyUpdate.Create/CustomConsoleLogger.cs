using System;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Logging.Loggers;

namespace TinyUpdate.Create
{
    /// <summary>
    /// <see cref="ConsoleLogger"/> which also contains WriteLine and Write as <see cref="Console"/>
    /// </summary>
    public class CustomConsoleLogger : ConsoleLogger
    {
        public CustomConsoleLogger(string name) : base(name)
        {
        }

        /// <inheritdoc cref="Console.WriteLine(string, object[])"/>
        public void WriteLine(string message = "", params object?[] propertyValues)
        {
            WriteFormattedMessage(message + Environment.NewLine, propertyValues);
        }

        /// <inheritdoc cref="Console.Write(string, object[])"/>
        public void Write(string message, params object?[] propertyValues)
        {
            WriteFormattedMessage(message, propertyValues);
        }
    }

    /// <summary>
    /// Builder to create <see cref="CustomLoggerBuilder"/>
    /// </summary>
    public class CustomLoggerBuilder : LoggingBuilder
    {
        /// <inheritdoc cref="LoggingBuilder.CreateLogger"/>
        public override ILogging CreateLogger(string name) => new CustomConsoleLogger(name);
    }
}