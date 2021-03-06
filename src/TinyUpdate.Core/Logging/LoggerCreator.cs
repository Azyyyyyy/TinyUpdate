﻿using System;
using System.Collections.Generic;
using System.Linq;
using TinyUpdate.Core.Logging.Loggers;

namespace TinyUpdate.Core.Logging
{
    /// <summary>
    /// Provides easy access to <see cref="ILogging"/>'s 
    /// </summary>
    public static class LoggingCreator
    {
        private static readonly List<LoggingBuilder> LogBuilders = new();

        /// <summary>
        /// How much logging we should process when not set in the <see cref="ILogging"/>
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public static LogLevel GlobalLevel { get; set; } = LogLevel.Info;

        /// <summary>
        /// If some kind of logging should be processed
        /// </summary>
        /// <param name="loggerLevel">What the logger <see cref="LogLevel"/> currently is</param>
        /// <param name="logLevel">What the log that we might process is</param>
        public static bool ShouldProcess(LogLevel? loggerLevel, LogLevel logLevel)
        {
            return logLevel >= (loggerLevel ?? GlobalLevel);
        }

        /// <summary>
        /// Creates <see cref="ILogging"/>
        /// </summary>
        /// <param name="name">Name of the class that is requesting an <see cref="ILogging"/></param>
        public static ILogging CreateLogger(string name)
        {
            var logger = new WrapperLogger(name,
                LogBuilders.Any() ? LogBuilders.ToArray() : new LoggingBuilder[] {new EmptyLoggerBuilder()});
            Loggers.Add(logger);
            return logger;
        }

        private static readonly List<ILogging> Loggers = new List<ILogging>();
        /// <summary>
        /// This lets you manually dispose any loggers that also have <see cref="IDisposable"/>
        /// </summary>
        public static void DisposeLoggers()
        {
            foreach (var logger in Loggers)
            {
                if (logger is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        
        /// <summary>
        /// Adds a <see cref="LoggingBuilder"/> that will be used when creating a <see cref="ILogging"/> from <see cref="CreateLogger"/>
        /// </summary>
        /// <param name="builder"><see cref="LoggingBuilder"/> to use</param>
        public static void AddLogBuilder(LoggingBuilder builder)
        {
            LogBuilders.Add(builder);
        }
    }
}