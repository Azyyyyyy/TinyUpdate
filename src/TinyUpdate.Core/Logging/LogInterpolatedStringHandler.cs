using System;
using System.Runtime.CompilerServices;
using TinyUpdate.Core.Logging.StringHandlers;

namespace TinyUpdate.Core.Logging;

#if NET6_0_OR_GREATER
[InterpolatedStringHandler]
public readonly ref struct LogInterpolatedStringHandler
{
    private readonly ILogInterpolatedStringHandler _handlerToUse;
    
    /// <summary>
    /// If this StringHandler is valid
    /// </summary>
    public bool IsValid { get; }
    
    /// <summary>
    /// If we had to fallback to using <see cref="ObjectStringHandler"/> (Due to a <see cref="ILogInterpolatedStringHandler"/> not being created)
    /// </summary>
    public bool ObjectFallback { get; }

    internal LogInterpolatedStringHandler(ILogInterpolatedStringHandler? handler)
    {
        IsValid = handler != null && handler != EmptyStringHandler.Handler;
        _handlerToUse = handler ?? EmptyStringHandler.Handler;
    }
    
    public LogInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logging, Level level, out bool handlerIsValid)
    {
        IsValid = handlerIsValid = LogManager.ShouldProcess(logging.LogLevel, level);
        var handlerToUse = logging.MakeStringHandler(level, literalLength, formattedCount);
        if (handlerToUse == null)
        {
            handlerToUse = new ObjectStringHandler(literalLength, formattedCount);
            ObjectFallback = true;
        }

        _handlerToUse = handlerToUse;
    }

    public void AppendLiteral(string s) => _handlerToUse.AppendLiteral(s);

    public void AppendFormatted<T>(T t, string? format) => _handlerToUse.AppendFormatted(t, format);
    public void AppendFormatted<T>(T t, Type type, string? format) => _handlerToUse.AppendFormatted(t, type, format);
    public void AppendFormatted<T>(T t) => _handlerToUse.AppendFormatted(t, typeof(T));
    public void AppendFormatted<T>(T t, Type type) => _handlerToUse.AppendFormatted(t, type);
    public override string? ToString() => _handlerToUse.ToString();

    public T? GetHandler<T>()
        where T : ILogInterpolatedStringHandler
    {
        if (_handlerToUse is T t)
        {
            return t;
        }

        return default;
    }
}
#endif