using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TinyUpdate.Core.Extensions;

namespace TinyUpdate.Core.Logging.StringHandlers;

public sealed class ObjectStringHandler : ILogInterpolatedStringHandler
{
    private readonly StringBuilder _stringBuilder;
    private readonly List<object> _prams = new();
    private bool _lastAddWasNewChars;

    private string? _finalMessage;
    private object?[]? _finalPrams;
    public ObjectStringHandler(int literalLength, int formattedCount)
    {
        _stringBuilder = new StringBuilder(StringStringHandler.GetDefaultLength(literalLength, formattedCount));
    }
    
    public object?[]? Prams => _finalPrams ??= _prams.Any() ? _prams.ToArray() : null;

    public void AppendLiteral(string s)
    {
        _stringBuilder.Append(s);
        _lastAddWasNewChars = s.EndsWithNewChars();
    }

    public void AppendFormatted<T>(T t, string? format) => AppendFormatted(t, typeof(T), format);
    public void AppendFormatted<T>(T t, Type type, string? format)
    {
        //If format contains nothing then process it like normal
        if (string.IsNullOrWhiteSpace(format))
        {
            AppendFormatted(t, type);
            return;
        }

        if (t is IFormattable formattable)
        {
            AppendFormatted(formattable.ToString(format, null), type);
            return;
        }
        
        //If it's not directly a IFormattable then see if string.Format can format it
        AppendFormatted(t == null ? null : string.Format(format, t), type);
    }

    public void AppendFormatted<T>(T t) => AppendFormatted(t, typeof(T));
    public void AppendFormatted<T>(T t, Type _)
    {
        var itemIndex = _prams.IndexOf(t!);
        if (itemIndex == -1)
        {
            itemIndex = _prams.Count;
            _prams.Add(t!);
        }

        _lastAddWasNewChars = t switch
        {
            char ch => ch.ContainsNewChars(),
            string str => str.EndsWithNewChars(),
            _ => _lastAddWasNewChars
        };

        _stringBuilder.Append("{" + itemIndex + "}");
    }
    
    public string GetStringAndClear()
    {
        if (_finalMessage != null)
        {
            return _finalMessage;
        }
        
        if (!_lastAddWasNewChars)
        {
            _stringBuilder.Append(Environment.NewLine);
        }

        _finalMessage = _stringBuilder.ToString();
        _stringBuilder.Clear();
        return _finalMessage;
    }
}