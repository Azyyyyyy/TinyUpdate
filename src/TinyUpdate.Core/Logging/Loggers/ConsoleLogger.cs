using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TinyUpdate.Core.Extensions;

namespace TinyUpdate.Core.Logging.Loggers
{
    //TODO: Fix issue where output will sometimes be skipped
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
            Write("DEBUG", ConsoleColor.Blue, Logging.LogLevel.Trace, message, propertyValues);
        }

        /// <inheritdoc cref="ILogging.Error(string, object[])"/>
        public void Error(string message, params object?[] propertyValues)
        {
            Write("ERROR", ConsoleColor.Red, Logging.LogLevel.Error, message, propertyValues);
        }

        /// <inheritdoc cref="ILogging.Error(Exception, object[])"/>
        public void Error(Exception e, params object?[] propertyValues)
        {
            Error(e.Message, propertyValues);
        }

        /// <inheritdoc cref="ILogging.Information"/>
        public void Information(string message, params object?[] propertyValues)
        {
            Write("INFO", ConsoleColor.Cyan, Logging.LogLevel.Info, message, propertyValues);
        }

        /// <inheritdoc cref="ILogging.Warning"/>
        public void Warning(string message, params object?[] propertyValues)
        {
            Write("WARNING", ConsoleColor.Yellow, Logging.LogLevel.Warn, message, propertyValues);
        }

        /// <summary>
        /// Writes data to <see cref="Console"/>
        /// </summary>
        /// <param name="type">What type of log this is (Debug, Info, Error etc...)</param>
        /// <param name="colour">Colour to be used for [TYPE - NAME]</param>
        /// <param name="logLevel">The log level that this is</param>
        /// <param name="message">Message to output</param>
        /// <param name="propertyValues">objects that should be formatted into the outputted message</param>
        private void Write(string type, ConsoleColor colour, LogLevel logLevel, string message,
            params object?[] propertyValues)
        {
            if (!LoggingCreator.ShouldProcess(LogLevel, logLevel))
            {
                return;
            }

            WriteInit(type, colour, logLevel, message, propertyValues);
        }
        
        private readonly List<CancellationTokenSource> _writeGuid = new();
        private async void WriteInit(string type, ConsoleColor colour, LogLevel logLevel, string message,
            params object?[] propertyValues)
        {
            var token = new CancellationTokenSource();
            _writeGuid.Add(token);
            if (_writeGuid.Count > 1)
            {
                await token.Token.Wait();
            }

            _writeGuid.Remove(token);

            var oldColour = Console.ForegroundColor;
            Console.ForegroundColor = colour;
            if (Console.CursorLeft != 0)
            {
                Console.Write(Environment.NewLine);
            }

            Console.Write($"[{type} - {Name}]: ");

            Console.ForegroundColor = oldColour;
            WriteMessage(message, true, false, false, propertyValues);
            _writeGuid.FirstOrDefault()?.Cancel();
        }

        protected void WriteMessage(string message, bool writeNewLineOnEnd, bool checkCursor, bool processWaitCheck,
            params object?[] propertyValues)
        {
            WriteMessageInit(message, writeNewLineOnEnd, checkCursor, processWaitCheck, propertyValues);
        }

        private async void WriteMessageInit(string message, bool writeNewLineOnEnd, bool checkCursor,
            bool processWaitCheck, params object?[] propertyValues)
        {
            var token = new CancellationTokenSource();
            if (processWaitCheck)
            {
                _writeGuid.Add(token);
                if (_writeGuid.Count > 1)
                {
                    await token.Token.Wait();
                }

                _writeGuid.Remove(token);
            }

            if (checkCursor && Console.CursorLeft != 0)
            {
                Console.Write(Environment.NewLine);
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine();
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
                    if (writeNewLineOnEnd)
                    {
                        Console.WriteLine(message);
                        break;
                    }

                    Console.Write(message);
                    break;
                }

                Console.Write(message[..(startBracketInt - 1)]);
                if (!int.TryParse(message[startBracketInt..endBracketInt], out var number))
                {
                    throw new FormatException();
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(propertyValues[number]);
                Console.ForegroundColor = oldColour;
                message = message.Substring(endBracketInt + 1, message[(endBracketInt + 1)..].Length);
            }

            if (processWaitCheck)
            {
                _writeGuid.FirstOrDefault()?.Cancel();
            }
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