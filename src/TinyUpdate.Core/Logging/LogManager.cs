using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging.Loggers;

namespace TinyUpdate.Core.Logging;
/// <summary>
/// Manages the creation and handling of <see cref="ILogger"/>'s 
/// </summary>
public static class LogManager
{
    private static readonly object LogLock = new object();
    private static readonly List<ILogger> Loggers = new List<ILogger>();
    internal static LogBuilder[] LogBuilders = Array.Empty<LogBuilder>();

    private static readonly Lazy<ILogger> Logger = new Lazy<ILogger>(() => CreateLogger(nameof(LogManager)));
    static LogManager()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Dispose();
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            //We don't want to handle non-terminating exception's from here
            if (!args.IsTerminating)
            {
                return;
            }

            var logger = Logger.Value;
            logger.Error("Something happened which will crash the application!");
            if (args.ExceptionObject is Exception e)
            {
                logger.Log(e);
            }
            else
            {
                logger.Log(Level.Error, $"{args.ExceptionObject}");
            }
                
            Dispose();
        };
    }
    
    /// <summary>
    /// How much logging we should process when not set in the <see cref="ILogger"/>
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static Level GlobalLevel { get; set; } = Level.Info;

    /// <summary>
    /// Registers when an application is shutting down and we should dispose of our <see cref="ILogger"/>'s
    /// </summary>
    /// <param name="token"></param>
    public static void RegisterShutdown(CancellationToken token)
    {
        token.Register(Dispose);
    }
    
    /// <summary>
    /// If some kind of logging should be processed
    /// </summary>
    /// <param name="loggerLevel">What the logger <see cref="Level"/> currently is</param>
    /// <param name="logLevel">What the log that we might process is</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldProcess(Level? loggerLevel, Level logLevel) => logLevel >= (loggerLevel ?? GlobalLevel);

    /// <summary>
    /// Creates a <see cref="ILogger"/>
    /// </summary>
    /// <param name="name">Name of the class that is requesting an <see cref="ILogger"/></param>
    public static ILogger CreateLogger(string name)
    {
        if (LogBuilders.LongLength == 0)
        {
            return EmptyLogger.StaticLogger;
        }

        ILogger logger;
        lock (LogLock)
        {
            //We don't want to keep creating the same logger when we can reuse one already made
            var existingLogger = Loggers.FirstOrDefault(x => x.Name == name);
            if (existingLogger != null)
            {
                return existingLogger;
            }
        
            //We want to wrap the loggers if we have multiple, else just create the logger directly
            logger = LogBuilders.LongLength switch
            {
                1 => LogBuilders[0].CreateLogger(name),
                _ => new WrapperLogger(name, LogBuilders)
            };

            Loggers.Add(logger);
        }
        return logger;
    }
        
    /// <summary>
    /// This manually disposes of the loggers when the application is shutting down or crashing
    /// </summary>
    private static void Dispose()
    {
        Loggers.OfType<IDisposable>().Concat(LogBuilders.OfType<IDisposable>()).ForEach(x => x.Dispose());        Loggers.Clear();
        Loggers.TrimExcess();
    }
        
    /// <summary>
    /// Adds a <see cref="LogBuilder"/> that will be used when creating a <see cref="ILogger"/> from <see cref="CreateLogger"/>
    /// </summary>
    /// <param name="builder"><see cref="LogBuilder"/> to use</param>
    public static void AddLogBuilder(LogBuilder builder)
    {
        var index = LogBuilders.Length;
        Array.Resize(ref LogBuilders, index + 1);

        LogBuilders[index] = builder;
    }
    
    /// <summary>
    /// Adds multiple <see cref="LogBuilder"/>'s that will be used when creating a <see cref="ILogger"/> from <see cref="CreateLogger"/>
    /// </summary>
    /// <param name="builders"><see cref="LogBuilder"/>'s to use</param>
    public static void AddLogBuilders(params LogBuilder[] builders)
    {
        var index = LogBuilders.Length;
        Array.Resize(ref LogBuilders, index + builders.Length);

        foreach (var builder in builders)
        {
            LogBuilders[index++] = builder;
        }
    }
}