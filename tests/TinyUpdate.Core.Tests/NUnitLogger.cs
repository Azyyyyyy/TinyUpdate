using Microsoft.Extensions.Logging;

namespace TinyUpdate.Core.Tests;

public class NUnitLogger<T> : ILogger<T>
{
    private NUnitLogger() { }
    
    public static readonly NUnitLogger<T> Instance = new NUnitLogger<T>();
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!string.IsNullOrWhiteSpace(eventId.Name) || eventId.Id > 0)
        {
            TestContext.WriteLine($"[{logLevel.ToString()}]: {formatter(state, exception)} (Event '{eventId.Name}', ID {eventId.Id})");
            return;
        }
        
        TestContext.WriteLine($"[{logLevel.ToString()}]: {formatter(state, exception)} (Unknown Event)");
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) 
        where TState : notnull
    {
        throw new NotImplementedException();
    }
}