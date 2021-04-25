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
        public static ILogging CreateLogger(string name) =>
            new WrapperLogger(name,
                LogBuilders.Any() ? LogBuilders.ToArray() : new LoggingBuilder[] {new EmptyLoggerBuilder()});

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