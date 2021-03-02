namespace TinyUpdate.Core.Logging
{
    /// <summary>
    /// Builder for a <see cref="ILogging"/> to be created within <see cref="Logging"/>
    /// </summary>
    public abstract class LoggingBuilder
    {
        /// <summary>
        /// Creates <see cref="ILogging"/>
        /// </summary>
        /// <param name="name">Name of the class that is requesting an <see cref="ILogging"/></param>
        public abstract ILogging CreateLogger(string name);
    }
}