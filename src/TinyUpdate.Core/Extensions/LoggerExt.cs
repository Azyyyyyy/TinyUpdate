using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Core.Extensions;

public static partial class LoggerExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Debug(this ILogger logger, string message) => logger.Log(Level.Trace, message);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Debug(this ILogger logger, string message, object?[]? prams) => logger.Log(Level.Trace, message, prams);

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(this ILogger logger, string message) => logger.Log(Level.Info, message);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(this ILogger logger, string message, params object?[]? prams) => logger.Log(Level.Info, message, prams);

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warn(this ILogger logger, string message) => logger.Log(Level.Warn, message);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warn(this ILogger logger, string message, params object?[]? prams) => logger.Log(Level.Warn, message, prams);
}

public static partial class LoggerExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(this ILogger logger, string message) => logger.Log(Level.Error, message);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(this ILogger logger, string message, params object?[]? prams) => logger.Log(Level.Error, message, prams);
    
    public static string GetShortCode(this Level level)
    {
        return level switch
        {
            Level.Trace => "DEBUG",
            Level.Info => "INFO",
            Level.Warn => "WARN",
            Level.Error => "ERROR",
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
    }

    //TODO: Make a better version of this
    public static string MakeExceptionMessage(this Exception e)
    {
        return e.Message;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanUsePrams([NotNullWhen(true)] this object?[]? prams) => prams != null && prams.Any();
}