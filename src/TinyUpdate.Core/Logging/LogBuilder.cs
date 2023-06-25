namespace TinyUpdate.Core.Logging;
/// <summary>
/// Builder for a <see cref="ILogger"/> to be easily created
/// </summary>
public abstract class LogBuilder
{
    /// <summary>
    /// Creates <see cref="ILogger"/>
    /// </summary>
    /// <param name="name">Name of the class that is requesting an <see cref="ILogger"/></param>
    public abstract ILogger CreateLogger(string name);
}