using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TinyUpdate.Core.Extensions;

namespace TinyUpdate.Core.Logging.Loggers
{
    /// <summary>
    /// Logs to a file on disk
    /// </summary>
    public class FileLogger : ILogging, IDisposable
    {
        internal Lazy<TextWriter> _fileWriter;
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

        //For how many places currently have this stored
        private readonly object _counterLock = new object();
        private int _counter;
        internal int Counter
        {
            get
            {
                lock (_counterLock)
                {
                    return _counter;
                }
            }
            set
            {
                lock (_counterLock)
                {
                    _counter = value;
                }
            }
        }

        ~FileLogger()
        {
            Dispose();
        }

        private bool _disposed;
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            if (_fileWriter.IsValueCreated)
            {
                _fileWriter.Value.Flush();
            }

            //If no-one else is using this then dispose of it
            Counter--;
            if (Counter <= 0
            && _fileWriter.IsValueCreated)
            {
                _fileWriter.Value.Dispose();
                GC.SuppressFinalize(this);
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

        private readonly List<FileLogger> _loggers = new List<FileLogger>();
        /// <inheritdoc cref="LoggingBuilder.CreateLogger"/>
        public override ILogging CreateLogger(string name)
        {
            var logger = _loggers.FirstOrDefault(x => x.Name == name);
            if (logger == null)
            {
                logger = new FileLogger(name, Path.Combine(_dir, name), _time.ToFileName() + ".log");
                _loggers.Add(logger);
            }
            logger.Counter++;
            return logger;
        }

        public void Dispose()
        {
            _loggers.ForEach(x => x.Dispose());
        }
    }
}