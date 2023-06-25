using System;
using System.Runtime.CompilerServices;

namespace TinyUpdate.Core.Logging.Loggers;
public sealed class EmptyLogger : ILogger
{
    private EmptyLogger() { }
    
    public static EmptyLogger StaticLogger { get; } = new EmptyLogger();
    public string Name => "";
    public Level? LogLevel { get; set; }
    public bool HasStringHandler => false;

    public ILogInterpolatedStringHandler? MakeStringHandler(Level level, int literalLength, int formattedCount) => null;

    public void Log(Exception e) { }
    public void Log(Level level, string message) { }
    public void Log(Level level, string message, object?[]? prams) { }
    
#if NET6_0_OR_GREATER
    public void Log(Level level, [InterpolatedStringHandlerArgument("", "level")] LogInterpolatedStringHandler builder) { }
#endif
}