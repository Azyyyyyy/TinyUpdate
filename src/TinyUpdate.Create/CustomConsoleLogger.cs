using System;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Logging.Loggers;

namespace TinyUpdate.Create
{
    public class CustomConsoleLogger : ConsoleLogger
    {
        public CustomConsoleLogger(string name) : base(name)
        {
        }

        public void WriteLine(string message, params object[] propertyValues)
        {
            Console.WriteLine(message, propertyValues);
        }

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
    /// Builder to create <see cref="LoggingBuilder"/>
    /// </summary>
    public class CustomLoggerBuilder : LoggingBuilder
    {
        /// <inheritdoc cref="LoggingBuilder.CreateLogger"/>
        public override ILogging CreateLogger(string name) => new CustomConsoleLogger(name);
    }
}