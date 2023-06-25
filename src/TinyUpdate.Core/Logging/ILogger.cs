using System;
using System.Runtime.CompilerServices;

namespace TinyUpdate.Core.Logging;

/// <summary>
/// Interface for providing logging
/// </summary>
public interface ILogger
{
    /// <summary>
    /// The name of class that is using the <see cref="ILogger"/>
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The level that we should process the log (If null then <see cref="LogManager.GlobalLevel"/> is to be used)
    /// </summary>
    Level? LogLevel { get; set; }

    bool HasStringHandler { get; }

    /// <summary>
    /// String Handler to use for InterpolatedStringHandler calls
    /// </summary>
    public ILogInterpolatedStringHandler? MakeStringHandler(Level level, int literalLength, int formattedCount);

    /// <summary>
    /// Writes error data (With details contained in exception)
    /// </summary>
    /// <param name="e">Exception to use</param>
    void Log(Exception e);
    void Log(Level level, string message);
    void Log(Level level, string message, object?[]? prams);

#if NET6_0_OR_GREATER
    void Log(Level level, [InterpolatedStringHandlerArgument("", "level")] LogInterpolatedStringHandler builder);
#endif
}