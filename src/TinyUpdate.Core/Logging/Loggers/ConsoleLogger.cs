using System;
using System.Linq;
using TinyUpdate.Core.Extensions;

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

        public LogLevel? LogLevel { get; set; }

        /// <inheritdoc cref="ILogging.Debug"/>
        public void Debug(string message, params object?[] propertyValues)
        {
            WriteLine("DEBUG", ConsoleColor.Blue, Logging.LogLevel.Trace, message, propertyValues);
        }

        /// <inheritdoc cref="ILogging.Error(string, object[])"/>
        public void Error(string message, params object?[] propertyValues)
        {
            WriteLine("ERROR", ConsoleColor.Red, Logging.LogLevel.Error, message, propertyValues);
        }

        /// <inheritdoc cref="ILogging.Error(Exception, object[])"/>
        public void Error(Exception e, params object?[] propertyValues)
        {
            Error(e.Message + this.GetPropertyDetails(propertyValues), propertyValues);
        }
        
        /// <inheritdoc cref="ILogging.Information"/>
        public void Information(string message, params object?[] propertyValues)
        {
            WriteLine("INFO", ConsoleColor.Cyan, Logging.LogLevel.Info, message, propertyValues);
        }

        /// <inheritdoc cref="ILogging.Warning"/>
        public void Warning(string message, params object?[] propertyValues)
        {
            WriteLine("WARNING", ConsoleColor.Yellow, Logging.LogLevel.Warn, message, propertyValues);
        }

        /// <summary>
        /// Writes data to <see cref="Console"/>
        /// </summary>
        /// <param name="type">What type of log this is (Debug, Info, Error etc...)</param>
        /// <param name="colour">Colour to be used for [TYPE - NAME]</param>
        /// <param name="logLevel">The log level that this is</param>
        /// <param name="message">Message to output</param>
        /// <param name="propertyValues">objects that should be formatted into the outputted message</param>
        private void WriteLine(string type, ConsoleColor colour, LogLevel logLevel, string message,
            params object?[] propertyValues)
        {
            if (!LoggingCreator.ShouldProcess(LogLevel, logLevel))
            {
                return;
            }

            //If the output is being outputted then we can't
            //set the colours so no need to do all the fancy logic for colouring
            if (Console.IsOutputRedirected)
            {
                lock (WriteLineLock)
                {
                    Console.Out.WriteLine($"[{type} - {Name}]: " + string.Format(message, propertyValues));
                    return;
                }
            }

            lock (WriteLineLock)
            {
                var oldColour = Console.ForegroundColor;
                Console.ForegroundColor = colour;
                Console.Out.Write($"[{type} - {Name}]: ");
                Console.ForegroundColor = oldColour;

                WriteFormattedMessage(message + Environment.NewLine, propertyValues);
            }
        }

        private static readonly object WriteLineLock = new object();
        private static readonly object WriteLock = new object();
        // ReSharper disable once MemberCanBePrivate.Global
        protected static void WriteFormattedMessage(string message, params object?[] propertyValues)
        {
            lock (WriteLock)
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    return;
                }

                var oldColour = Console.ForegroundColor;
                while (message.Length != 0)
                {
                    var startBracketInt = message.IndexOf('{') + 1;
                    var endBracketInt = message.IndexOf('}');
                    /*This shows that we are at the end of the message
                     or the message has no properties to show*/
                    if (startBracketInt == 0 && endBracketInt == -1)
                    {
                        Console.Out.Write(message);
                        return;
                    }

                    Console.Out.Write(message[..(startBracketInt - 1)]);
                    if (!int.TryParse(message[startBracketInt..endBracketInt], out var number))
                    {
                        throw new FormatException();
                    }

                    Console.ForegroundColor = GetColourBasedOnType(propertyValues[number]);
                    Console.Out.Write(propertyValues[number] ?? "null");
                    Console.ForegroundColor = oldColour;
                    message = message.Substring(endBracketInt + 1, message[(endBracketInt + 1)..].Length);
                }
            }
        }

        private static ConsoleColor GetColourBasedOnType(object? o)
        {
            return o switch
            {
                null => ConsoleColor.Blue,
                bool => ConsoleColor.Blue,
                string => ConsoleColor.Cyan,
                _ when IsNumber(o) => ConsoleColor.Magenta,
                _ => ConsoleColor.Green
            };
        }

        private static bool IsNumber(object o)
        {
#if NET6_0_OR_GREATER
            return o.GetType().GetInterfaces().Any(x => (x.Namespace + "." + x.Name) == "System.INumber`1");
#else
            if (o is not ValueType)
            {
                return false;
            }
            return o is int or uint or long or ulong or decimal or byte 
                or sbyte or short or ushort or double or float;
#endif
        }
    }

    /// <summary>
    /// Builder to create <see cref="ConsoleLogger"/>
    /// </summary>
    public class ConsoleLoggerBuilder : LoggingBuilder
    {
        /// <inheritdoc cref="LoggingBuilder.CreateLogger"/>
        public override ILogging CreateLogger(string name) => new ConsoleLogger(name);
    }
}