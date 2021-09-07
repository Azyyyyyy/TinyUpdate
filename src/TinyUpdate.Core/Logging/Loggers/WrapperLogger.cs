using System;
using System.Linq;

namespace TinyUpdate.Core.Logging.Loggers
{
    /// <summary>
    /// Logger that wraps around multiple loggers (Building the loggers itself)
    /// </summary>
    internal sealed class WrapperLogger : ILogging, IDisposable
    {
        private readonly ILogging[] _loggers;

        public WrapperLogger(string name, params LoggingBuilder[] builders)
        {
            _loggers = new ILogging[builders.LongLength];
            for (var i = 0; i < builders.LongLength; i++)
            {
                _loggers[i] = builders[i].CreateLogger(name);
            }
        }

        /// <inheritdoc cref="ILogging.Name"/>
        public string Name => nameof(WrapperLogger);

        public LogLevel? LogLevel { get; set; }

        /// <inheritdoc cref="ILogging.Debug"/>
        public void Debug(string message, params object?[] propertyValues)
        {
            foreach (var logger in _loggers)
            {
                logger.Debug(message, propertyValues);
            }
        }

        /// <inheritdoc cref="ILogging.Error(string, object[])"/>
        public void Error(string message, params object?[] propertyValues)
        {
            foreach (var logger in _loggers)
            {
                logger.Error(message, propertyValues);
            }
        }

        /// <inheritdoc cref="ILogging.Error(Exception, object[])"/>
        public void Error(Exception e, params object?[] propertyValues)
        {
            foreach (var logger in _loggers)
            {
                logger.Error(e, propertyValues);
            }
        }

        /// <inheritdoc cref="ILogging.Information"/>
        public void Information(string message, params object?[] propertyValues)
        {
            foreach (var logger in _loggers)
            {
                logger.Information(message, propertyValues);
            }
        }

        /// <inheritdoc cref="ILogging.Warning"/>
        public void Warning(string message, params object?[] propertyValues)
        {
            foreach (var logger in _loggers)
            {
                logger.Warning(message, propertyValues);
            }
        }

        private bool _disposed;
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            
            foreach (var disposable in _loggers.Select(x => x as IDisposable))
            {
                disposable?.Dispose();
            }
            Array.Clear(_loggers, 0, _loggers.Length);
            GC.SuppressFinalize(this);
        }

        ~WrapperLogger()
        {
            Dispose();
        }
    }
}