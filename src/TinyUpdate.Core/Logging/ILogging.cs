using System;

namespace TinyUpdate.Core.Logging
{
    /// <summary>
    /// Interface for providing logging
    /// </summary>
    public interface ILogging
    {
        /// <summary>
        /// The name of class that's using this logger
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Writes debug data
        /// </summary>
        /// <param name="message">Message to write</param>
        /// <param name="propertyValues">objects that should be formatted into the outputted message</param>
        void Debug(string message, params object[] propertyValues);

        /// <summary>
        /// Writes information data
        /// </summary>
        /// <param name="message">Message to write</param>
        /// <param name="propertyValues">objects that should be formatted into the outputted message</param>
        void Information(string message, params object[] propertyValues);

        /// <summary>
        /// Writes warning data
        /// </summary>
        /// <param name="message">Message to write</param>
        /// <param name="propertyValues">objects that should be formatted into the outputted message</param>
        void Warning(string message, params object[] propertyValues);

        /// <summary>
        /// Writes error data
        /// </summary>
        /// <param name="message">Message to write</param>
        /// <param name="propertyValues">objects that should be formatted into the outputted message</param>
        void Error(string message, params object[] propertyValues);

        /// <summary>
        /// Writes error data (With details contained in exception)
        /// </summary>
        /// <param name="e">Exception to use</param>
        /// <param name="propertyValues">objects that should be formatted into the outputted message</param>
        void Error(Exception e, params object[] propertyValues);
    }
}
