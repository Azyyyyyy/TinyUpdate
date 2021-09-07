using System;
using System.Collections.Generic;
using System.IO;
using TinyUpdate.Core.Extensions;

namespace TinyUpdate.Core.Logging.Loggers
{
    /// <summary>
    /// Logs to a file on disk
    /// </summary>
    public class FileLogger : ILogging, IDisposable
    {
        private readonly Lazy<TextWriter> _fileWriter;
        public FileLogger(string name, string dir, string file)
        {
            Name = name;
            _fileWriter = new Lazy<TextWriter>(() =>
            {
                Directory.CreateDirectory(dir);
                return TextWriter.Synchronized(new StreamWriter(Path.Combine(dir, file)));
            });
        }

        /// <inheritdoc cref="ILogging.Name"/>
        public string Name { get; }

        public LogLevel? LogLevel { get; set; }

        /// <inheritdoc cref="ILogging.Debug"/>
        public void Debug(string message, params object?[] propertyValues)
        {
            if (LoggingCreator.ShouldProcess(LogLevel, Logging.LogLevel.Trace))
            {
                _fileWriter.Value.WriteLine("[DEBUG] " + message, propertyValues);
            }
        }

        /// <inheritdoc cref="ILogging.Error(string, object[])"/>
        public void Error(string message, params object?[] propertyValues)
        {
            if (LoggingCreator.ShouldProcess(LogLevel, Logging.LogLevel.Error))
            {
                _fileWriter.Value.WriteLine("[ERROR] " + message, propertyValues);
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
                _fileWriter.Value.WriteLine("[INFO] " + message, propertyValues);
            }
        }

        /// <inheritdoc cref="ILogging.Warning"/>
        public void Warning(string message, params object?[] propertyValues)
        {
            if (LoggingCreator.ShouldProcess(LogLevel, Logging.LogLevel.Warn))
            {
                _fileWriter.Value.WriteLine("[WARN] " + message, propertyValues);
            }
        }

        public void Dispose()
        {
            if (_fileWriter.IsValueCreated)
            {
                _fileWriter.Value.Flush();
            }
        }
    }

    /// <summary>
    /// Builder to create <see cref="FileLogger"/>
    /// </summary>
    public class FileLoggerBuilder : LoggingBuilder, IDisposable
    {
        private readonly DateTime _time = DateTime.Now;
        private readonly string _dir;
        public FileLoggerBuilder(string dir)
        {
            _dir = dir;
        }

        //TODO: Fix issue with making a logger with same name multiple times
        private readonly List<FileLogger> _loggers = new List<FileLogger>();
        /// <inheritdoc cref="LoggingBuilder.CreateLogger"/>
        public override ILogging CreateLogger(string name)
        {
            var logger = new FileLogger(name, Path.Combine(_dir, name), _time.ToFileName() + ".log");
            _loggers.Add(logger);
            return logger;
        }

        public void Dispose()
        {
            _loggers.ForEach(x => x.Dispose());
        }
    }
}