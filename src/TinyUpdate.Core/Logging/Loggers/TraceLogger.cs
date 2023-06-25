using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging.StringHandlers;

namespace TinyUpdate.Core.Logging.Loggers;
/// <summary>
/// Logs to <see cref="Trace"/>
/// </summary>
public sealed class TraceLogger : ILogger
{
    public TraceLogger(string name)
    {
        Name = name;
    }
    
    public string Name { get; }
    public Level? LogLevel { get; set; }
    public bool HasStringHandler => true;

    public ILogInterpolatedStringHandler MakeStringHandler
        (Level level, int literalLength, int formattedCount) => new StringStringHandler(literalLength, formattedCount);

    public void Log(Exception e) => Log(Level.Error, e.MakeExceptionMessage(), null);

    public void Log(Level level, string message) => Log(level, message, null);
    public void Log(Level level, string message, object?[]? prams)
    {
        if (LogManager.ShouldProcess(LogLevel, level))
        {
            message = (prams.CanUsePrams() ? string.Format(NullFormatProvider.FormatProvider, message, prams) : message).TrimEnd();
            Trace.WriteLine(message, $"{Name} ({level.GetShortCode()})");
        }
    }

#if NET6_0_OR_GREATER
    public void Log(Level level, [InterpolatedStringHandlerArgument("", "level")] LogInterpolatedStringHandler builder)
    {
        var message = builder.GetHandler<StringStringHandler>()?.GetStringAndClear() ?? "";
        Log(level, message, null);
    }
#endif
}

/// <summary>
/// Builder to create <see cref="TraceLogger"/>
/// </summary>
public sealed class TraceLoggerBuilder : LogBuilder
{
    /// <inheritdoc cref="LogBuilder.CreateLogger"/>
    public override ILogger CreateLogger(string name) => new TraceLogger(name);
}