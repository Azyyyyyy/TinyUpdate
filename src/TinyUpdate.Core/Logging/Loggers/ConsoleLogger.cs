using System;

namespace TinyUpdate.Core.Logging.Loggers
{
    /// <summary>
    /// Logs to the Console with some colour
    /// </summary>
    public class ConsoleLogger : ILogging
    {
        public ConsoleLogger(string name)
        {
            Name = name;
        }

        /// <inheritdoc cref="ILogging.Name"/>
        public string Name { get; }

        /// <inheritdoc cref="ILogging.Debug"/>
        public void Debug(string message, params object[] propertyValues)
        {
            Write("DEBUG", ConsoleColor.Blue, message, propertyValues);
        }

        /// <inheritdoc cref="ILogging.Error(string, object[])"/>
        public void Error(string message, params object[] propertyValues)
        {
            Write("ERROR", ConsoleColor.Red, message, propertyValues);
        }

        /// <inheritdoc cref="ILogging.Error(Exception, object[])"/>
        public void Error(Exception e, params object[] propertyValues)
        {
            Error(e.Message, propertyValues);
        }

        /// <inheritdoc cref="ILogging.Information"/>
        public void Information(string message, params object[] propertyValues)
        {
            Write("INFO", ConsoleColor.Cyan, message, propertyValues);
        }

        /// <inheritdoc cref="ILogging.Warning"/>
        public void Warning(string message, params object[] propertyValues)
        {
            Write("WARNING", ConsoleColor.Yellow, message, propertyValues);
        }
        
        /// <summary>
        /// Writes data to the <see cref="Console"/>
        /// </summary>
        /// <param name="type">What type of log this is (Debug, Info, Error etc...)</param>
        /// <param name="colour">Colour to be used for [TYPE - NAME]</param>
        /// <param name="message">Message to output</param>
        /// <param name="propertyValues">objects that should be formatted into the outputted message</param>
        protected virtual void Write(string type, ConsoleColor colour, string message, params object[] propertyValues)
        {
            var oldColour = Console.ForegroundColor;
            Console.ForegroundColor = colour;
            if (Console.CursorLeft != 0)
            {
                Console.Write(Environment.NewLine);
            }
            Console.Write($"[{type} - {Name}]: ");

            Console.ForegroundColor = oldColour;
            Console.WriteLine(message, propertyValues);
        }
    }

    /// <summary>
    /// Builder to create <see cref="LoggingBuilder"/>
    /// </summary>
    public class ConsoleLoggerBuilder : LoggingBuilder
    {
        /// <inheritdoc cref="LoggingBuilder.CreateLogger"/>
        public override ILogging CreateLogger(string name) => new ConsoleLogger(name);
    }
}