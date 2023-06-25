using System;

namespace TinyUpdate.Core.Logging;

public interface ILogInterpolatedStringHandler
{
    public void AppendLiteral(string s);
    
    //TODO: Get format provider overload?
    public void AppendFormatted<T>(T t, string? format);
    public void AppendFormatted<T>(T t, Type type, string? format);
    public void AppendFormatted<T>(T t);
    public void AppendFormatted<T>(T t, Type type);
}