using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging.StringHandlers;

namespace TinyUpdate.Core.Logging.Loggers;
/// <summary>
/// Logs to a file on disk
/// </summary>
public sealed class FileLogger : ILogger, IDisposable
{
    private bool _disposed;
    private readonly Lazy<TextWriter> _fileWriter;

    public FileLogger(string name, string dir, string file)
    {
        Name = name;
        _fileWriter = new Lazy<TextWriter>(() =>
        {
            Directory.CreateDirectory(dir);
            return TextWriter.Synchronized(new StreamWriter(Path.Combine(dir, file)));
        });
    }

    public string Name { get; }
    public Level? LogLevel { get; set; }
    public bool HasStringHandler => true;

    public ILogInterpolatedStringHandler MakeStringHandler(Level level, int literalLength, int formattedCount)
    {
        var strHandler = new StringStringHandler(literalLength, formattedCount);
        strHandler.AppendLiteral($"[{level.GetShortCode()} - {Name}]: ");
        return strHandler;
    }

    public void Log(Exception e) => Log(Level.Error, e.MakeExceptionMessage(), null);

    public void Log(Level level, string message) => Log(level, message, null);
    public void Log(Level level, string message, object?[]? prams)
    {
        if (_disposed || !LogManager.ShouldProcess(LogLevel, level))
        {
            return;
        }

        message = $"[{level.GetShortCode()} - {Name}]: {message.TrimEnd()}";
        if (prams.CanUsePrams())
        {
            /*We can't pass in NullFormatProvider so we have to recreate the array, not the best way
             but it's better then missing part of the log*/
            if (prams.Any(x => x == null))
            {
                var editedPrams = prams.Select(x => x ?? "null").ToArray();
                _fileWriter.Value.WriteLine(message, editedPrams);
            }
            else
            {
                _fileWriter.Value.WriteLine(message, prams);
            }
            return;
        }
                
        _fileWriter.Value.WriteLine(message);
    }

#if NET6_0_OR_GREATER
    public void Log(Level level, [InterpolatedStringHandlerArgument("", "level")] LogInterpolatedStringHandler builder)
    {
        if (!_disposed && builder.IsValid)
        {
            var message = builder.GetHandler<StringStringHandler>()?.GetStringAndClear();
            _fileWriter.Value.WriteLine(message);
        }
    }
#endif
    
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        if (_fileWriter.IsValueCreated)
        {
            _fileWriter.Value.Flush();
            _fileWriter.Value.Dispose();
        }
        GC.SuppressFinalize(this);
    }
    
    ~FileLogger() => Dispose();
}

/// <summary>
/// Builder to create <see cref="FileLogger"/>
/// </summary>
public sealed class FileLoggerBuilder : LogBuilder
{
    private readonly DateTime _time = DateTime.Now;
    private readonly string _dir;
    public FileLoggerBuilder(string dir)
    {
        _dir = dir;
        Directory.CreateDirectory(dir);
    }

    /// <inheritdoc cref="LogBuilder.CreateLogger"/>
    public override ILogger CreateLogger(string name) =>
        new FileLogger(name, Path.Combine(_dir, name), _time.ToFileName() + ".log");
}