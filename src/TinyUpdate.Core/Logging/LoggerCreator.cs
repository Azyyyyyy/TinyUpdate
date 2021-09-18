using System;
using System.Collections.Generic;
using TinyUpdate.Core.Logging.Loggers;

namespace TinyUpdate.Core.Logging
{
    /// <summary>
    /// Provides easy access to <see cref="ILogging"/>'s 
    /// </summary>
    public static class LoggingCreator
    {
        internal static LoggingBuilder[] _logBuilders = Array.Empty<LoggingBuilder>();
        static LoggingCreator()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                if (!args.IsTerminating)
                {
                    return;
                }

                Logger.Value.Error("Something happened which will crash the application!");
                if (args.ExceptionObject is Exception e)
                {
                    Logger.Value.Error(e);
                }
                else
                {
                    Logger.Value.Error("{0}", args.ExceptionObject);
                }
                
                Dispose();
            };
        }

        private static readonly Lazy<ILogging> Logger = new Lazy<ILogging>(() => CreateLogger("LoggerCreator"));

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
            ILogging logger = _logBuilders.LongLength switch
            {
                0 => EmptyLogger.StaticLogger,
                1 => _logBuilders[0].CreateLogger(name),
                _ => new WrapperLogger(name, _logBuilders)
            };

            Loggers.Add(logger);
            return logger;
        }
        
        private static readonly List<ILogging> Loggers = new List<ILogging>();
        /// <summary>
        /// This lets you manually dispose any created loggers (Only call this when your application is shutting down)
        /// </summary>
        private static void Dispose()
        {
            Loggers.ForEach(x =>
            {
                if (x is IDisposable d)
                {
                    d.Dispose();
                }
            });
            Loggers.Clear();
            Loggers.TrimExcess();
        }
        
        /// <summary>
        /// Adds a <see cref="LoggingBuilder"/> that will be used when creating a <see cref="ILogging"/> from <see cref="CreateLogger"/>
        /// </summary>
        /// <param name="builder"><see cref="LoggingBuilder"/> to use</param>
        public static void AddLogBuilder(LoggingBuilder builder)
        {
            var index = _logBuilders.Length;
            Array.Resize(ref _logBuilders, index + 1);
            
            _logBuilders[index] = builder;
        }
    }
}