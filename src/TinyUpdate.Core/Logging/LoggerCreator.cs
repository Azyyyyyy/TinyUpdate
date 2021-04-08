using System.Collections.Generic;
using System.Linq;
using TinyUpdate.Core.Logging.Loggers;

namespace TinyUpdate.Core.Logging
{
    //TODO: Have a LogLevel enum to control what gets shown
    /// <summary>
    /// Provides easy access to <see cref="ILogging"/>'s 
    /// </summary>
    public static class LoggingCreator
    {
        private static readonly List<LoggingBuilder> LogBuilders = new();
        
        /// <summary>
        /// Creates <see cref="ILogging"/>
        /// </summary>
        /// <param name="name">Name of the class that is requesting an <see cref="ILogging"/></param>
        public static ILogging CreateLogger(string name) => 
            new WrapperLogger(name, 
                LogBuilders.Any() ? LogBuilders.ToArray() : new LoggingBuilder[]{ new ConsoleLoggerBuilder() });

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