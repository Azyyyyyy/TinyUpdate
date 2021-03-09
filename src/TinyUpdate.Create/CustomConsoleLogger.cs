using System;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Logging.Loggers;

namespace TinyUpdate.Create
{
    /// <summary>
    /// Custom console logger that only shows warnings + errors
    /// </summary>
    public class CustomConsoleLogger : ConsoleLogger
    {
        public CustomConsoleLogger(string name) : base(name)
        {
        }

        /// <inheritdoc cref="Console.WriteLine(string, object[])"/>
        public void WriteLine(string message, params object[] propertyValues)
        {
            Console.WriteLine(message, propertyValues);
        }

        /// <inheritdoc cref="Console.Write(string, object[])"/>
        public void Write(string message, params object[] propertyValues)
        {
            Console.Write(message, propertyValues);
        }

        protected override void Write(string type, ConsoleColor colour, string message, params object[] propertyValues)
        {
            if (type == "WARNING" || type == "ERROR")
            {
                base.Write(type, colour, message, propertyValues);
            }
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