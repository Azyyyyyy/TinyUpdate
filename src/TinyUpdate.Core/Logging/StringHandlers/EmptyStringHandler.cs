using System;

namespace TinyUpdate.Core.Logging.StringHandlers;

public sealed class EmptyStringHandler : ILogInterpolatedStringHandler
{
    private EmptyStringHandler() { }

    public static EmptyStringHandler Handler { get; } = new();

    public void AppendLiteral(string s) { }
    public void AppendFormatted<T>(T t, string? format) { }
    public void AppendFormatted<T>(T t, Type type, string? format) { }
    public void AppendFormatted<T>(T t) { }
    public void AppendFormatted<T>(T t, Type type) { }
}