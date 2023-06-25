using System;
using System.Collections.Generic;
using System.Linq;
using TinyUpdate.Core.Extensions;

namespace TinyUpdate.Core.Logging.StringHandlers;

internal sealed class WrapperStringHandler : ILogInterpolatedStringHandler
{
    public WrapperStringHandler(IEnumerable<KeyValuePair<ILogger, ILogInterpolatedStringHandler?>> handlers, bool needObjectHandler, int literalLength, int formattedCount)
    {
        if (needObjectHandler)
        {
            handlers = handlers.Append(new(null!, new ObjectStringHandler(literalLength, formattedCount)));
        }
        Handlers = handlers.ToDictionary(x => x.Key, x => x.Value);
    }
    
    public Dictionary<ILogger, ILogInterpolatedStringHandler?> Handlers { get; }

    public void AppendLiteral(string s) => Handlers.ForEach(x => x.Value?.AppendLiteral(s));
    public void AppendFormatted<T>(T t, string? format) => Handlers.ForEach(x => x.Value?.AppendFormatted(t, format));
    public void AppendFormatted<T>(T t, Type type, string? format) => Handlers.ForEach(x => x.Value?.AppendFormatted(t, type, format));
    public void AppendFormatted<T>(T t) => Handlers.ForEach(x => x.Value?.AppendFormatted(t));
    public void AppendFormatted<T>(T t, Type type) => Handlers.ForEach(x => x.Value?.AppendFormatted(t, type));
}