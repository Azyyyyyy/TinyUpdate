using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging.StringHandlers;

namespace TinyUpdate.Core.Logging.Loggers;

/// <summary>
/// Logger that wraps around multiple loggers (Building the loggers itself)
/// </summary>
internal sealed class WrapperLogger : ILogger, IDisposable
{
    private readonly ILogger[] _loggers;
    private bool _disposed;
    private Level? _logLevel;

    public WrapperLogger(string name, params LogBuilder[] builders)
    {
        Name = name;
        _loggers = new ILogger[builders.LongLength];
        for (var i = 0; i < builders.LongLength; i++)
        {
            _loggers[i] = builders[i].CreateLogger(name);
        }
        LogLevel = null;
    }

    public string Name { get; }

    public Level? LogLevel
    {
        get => _logLevel;
        set
        {
            _logLevel = value;
            _loggers.ForEach(x => x.LogLevel = _logLevel);
        }
    }
    public bool HasStringHandler => true;

    public ILogInterpolatedStringHandler MakeStringHandler(Level level, int literalLength, int formattedCount)
    {
        var handlers = _loggers
            .Select(x => new KeyValuePair<ILogger, ILogInterpolatedStringHandler?>(x, x.HasStringHandler ? x.MakeStringHandler(level, literalLength, formattedCount) : EmptyStringHandler.Handler));
        
        return new WrapperStringHandler(handlers, _loggers.Any(x => !x.HasStringHandler), literalLength, formattedCount);
    }

    public void Log(Exception e) => _loggers.TakeWhile(x => !_disposed).ForEach(x => x.Log(e));

    public void Log(Level level, string message) => 
        _loggers.TakeWhile(x => !_disposed).ForEach(x => x.Log(level, message));

    public void Log(Level level, string message, object?[]? prams) => 
        _loggers.TakeWhile(x => !_disposed).ForEach(x => x.Log(level, message, prams));

#if NET6_0_OR_GREATER
    public void Log(Level level, [InterpolatedStringHandlerArgument("", "level")] LogInterpolatedStringHandler builder)
    {
        if (!builder.IsValid)
        {
            return;
        }

        var processedHandlers = builder.GetHandler<WrapperStringHandler>();
        if (processedHandlers == null)
        {
            return;
        }

        var objHandler = processedHandlers.Handlers.Any(x => x.Key == null) ? processedHandlers.Handlers[null!] : null;
        processedHandlers.Handlers.ForEach(x =>
        {
            var handler = x.Value == EmptyStringHandler.Handler ? objHandler : x.Value;
            switch (handler)
            {
                case EmptyStringHandler:
                    return;
                case ObjectStringHandler objectStringHandler:
                    x.Key.Log(level, objectStringHandler.GetStringAndClear(), objectStringHandler.Prams);
                    return;
                default:
                    x.Key.Log(level, new LogInterpolatedStringHandler(handler));
                    break;
            }
        });
    }
#endif

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        _loggers.OfType<IDisposable>().ForEach(d => d.Dispose());
        Array.Clear(_loggers, 0, _loggers.Length);
        GC.SuppressFinalize(this);
    }

    ~WrapperLogger() => Dispose();
}