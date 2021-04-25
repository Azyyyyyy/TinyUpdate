using System;
using System.Diagnostics;

namespace TinyUpdate.Core.Logging.Loggers
{
    /// <summary>
    /// Logs to <see cref="Trace"/>
    /// </summary>
    public class TraceLogger : ILogging
    {
        public TraceLogger(string name)
        {
            Name = name;
        }

        /// <inheritdoc cref="ILogging.Name"/>
        public string Name { get; }

        public LogLevel? LogLevel { get; set; }

        /// <inheritdoc cref="ILogging.Debug"/>
        public void Debug(string message, params object?[] propertyValues)
        {
            if (LoggingCreator.ShouldProcess(LogLevel, Logging.LogLevel.Trace))
            {
                Trace.WriteLine(string.Format(message, propertyValues), $"DEBUG - {Name}");
            }
        }

        /// <inheritdoc cref="ILogging.Error(string, object[])"/>
        public void Error(string message, params object?[] propertyValues)
        {
            if (LoggingCreator.ShouldProcess(LogLevel, Logging.LogLevel.Error))
            {
                Trace.TraceError(message + $" ({Name})", propertyValues);
            }
        }

        /// <inheritdoc cref="ILogging.Error(Exception, object[])"/>
        public void Error(Exception e, params object?[] propertyValues)
        {
            Error(e.Message, propertyValues);
        }

        /// <inheritdoc cref="ILogging.Information"/>
        public void Information(string message, params object?[] propertyValues)
        {
            if (LoggingCreator.ShouldProcess(LogLevel, Logging.LogLevel.Info))
            {
                Trace.TraceInformation(message + $" ({Name})", propertyValues);
            }
        }

        /// <inheritdoc cref="ILogging.Warning"/>
        public void Warning(string message, params object?[] propertyValues)
        {
            if (LoggingCreator.ShouldProcess(LogLevel, Logging.LogLevel.Warn))
            {
                Trace.TraceWarning(message + $" ({Name})", propertyValues);
            }
        }
    }

    /// <summary>
    /// Builder to create <see cref="TraceLogger"/>
    /// </summary>
    public class TraceLoggerBuilder : LoggingBuilder
    {
        /// <inheritdoc cref="LoggingBuilder.CreateLogger"/>
        public override ILogging CreateLogger(string name) => new TraceLogger(name);
    }
}