using System;

namespace TinyUpdate.Core.Logging;

public class NullFormatProvider : IFormatProvider
{
    private NullFormatProvider() { }
    public static NullFormatProvider FormatProvider { get; } = new NullFormatProvider();
    public object? GetFormat(Type? formatType) => NullFormatter.Formatter;
}

public class NullFormatter : ICustomFormatter 
{
    private NullFormatter() { }
    public static NullFormatter Formatter { get; } = new NullFormatter();

    public string Format(string? format, object? arg, IFormatProvider? formatProvider)
    {
        return arg == null ? "null" : null!;
    }
}