using System;
using System.Runtime.CompilerServices;
using System.Text;
using TinyUpdate.Core.Extensions;

namespace TinyUpdate.Core.Logging.StringHandlers;

public sealed class StringStringHandler : ILogInterpolatedStringHandler
{
    private readonly StringBuilder _stringBuilder;
    private bool _lastAddWasNewChars;
    public StringStringHandler(int literalLength, int formattedCount)
    {
        _stringBuilder = new StringBuilder(GetDefaultLength(literalLength, formattedCount));
    }

    //Taken from DefaultInterpolatedStringHandler
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetDefaultLength(int literalLength, int formattedCount) => Math.Max(256, literalLength + formattedCount * 11);
    
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
        
        //If it's not directly a IFormattable then see if string.Format can at format it
        AppendFormatted(t == null ? null : string.Format(format, t), type);
    }

    public void AppendFormatted<T>(T t) => AppendFormatted(t, typeof(T));
    public void AppendFormatted<T>(T t, Type type)
    {
        var mes = t?.ToString() ?? "null";
        _lastAddWasNewChars = mes.EndsWithNewChars();
        _stringBuilder.AppendFormat(mes);
    }

    public string GetStringAndClear()
    {
        if (!_lastAddWasNewChars)
        {
            _stringBuilder.Append(Environment.NewLine);
        }

        var str = _stringBuilder.ToString();
        _stringBuilder.Clear();
        return str;
    }
}