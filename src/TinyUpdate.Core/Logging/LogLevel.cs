namespace TinyUpdate.Core.Logging
{
    public enum LogLevel
    {
        /// <summary>
        /// Show all logging, even any debug loggin
        /// </summary>
        Trace,
        
        /// <summary>
        /// Show any general, warning, and error logging
        /// </summary>
        Info,
        
        /// <summary>
        /// Show any warnings and error logging
        /// </summary>
        Warn,

        /// <summary>
        /// Only show error logging
        /// </summary>
        Error
    }
}