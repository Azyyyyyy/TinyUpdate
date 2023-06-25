using System;
using System.Runtime.CompilerServices;
using Spectre.Console;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging.StringHandlers;

namespace TinyUpdate.Core.Logging.Loggers.Console;

public sealed class ConsoleLogger : ILogger, IDisposable
{
    private static readonly Queuer Queuer = new Queuer("Console Logging");
    private static readonly string ErrorCode = Level.Error.GetShortCode();

    private static readonly Style TitleStyle = new(decoration: Decoration.Bold);
    private static readonly Style TitleNameStyle = TitleStyle.Decoration(Decoration.Bold | Decoration.Italic);
    
    private static readonly Paragraph ErrorTitle = new Paragraph();

    private static readonly Color InfoColour = new Color(21, 230, 126);
    private static readonly Color TraceColour = new Color(17, 134, 212);
    private static readonly Color WarnColour = new Color(230, 125, 21);
    private static readonly Color ErrorColour = Color.Red;
    
    static ConsoleLogger()
    {
        Queuer.Start();
    }
    
    public ConsoleLogger(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public Level? LogLevel { get; set; }
    public bool HasStringHandler => true;

    public ILogInterpolatedStringHandler MakeStringHandler(Level level, int literalLength, int formattedCount)
    {
        /*If output is redirected then use this, we can't
          use fancy colour so no point in processing it that way*/
        if (System.Console.IsOutputRedirected)
        {
            var strHandler = new StringStringHandler(literalLength, formattedCount);
            strHandler.AppendLiteral($"[{level.GetShortCode()} - {Name}]: ");
            return strHandler;
        }
        
        var para = new Paragraph();
        AddTitle(para, level);
        return new ConsoleStringHandler(para);
    }

    public void Log(Exception e)
    {
        if (!LogManager.ShouldProcess(LogLevel, Level.Error))
        {
            return;
        }
        
        if (System.Console.IsOutputRedirected)
        {
            _ = Queuer.WaitForObject(() => 
                System.Console.Error.WriteLine($"[{ErrorCode} - {Name}]: {e.MakeExceptionMessage()}"));
            return;
        }
        
        _ = Queuer.WaitForObject(() =>
        {
            AnsiConsole.Write(ErrorTitle);
            AnsiConsole.Write(e.GetRenderable());
        });
    }

    public void Log(Level level, string message) => Log(level, message, null);
    public void Log(Level level, string message, object?[]? prams)
    {
        if (!LogManager.ShouldProcess(LogLevel, level))
        {
            return;
        }

        if (System.Console.IsOutputRedirected)
        {
            _ = Queuer.WaitForObject(() =>
            {
                var textStream = level == Level.Error ? System.Console.Error : System.Console.Out;
                message = (prams.CanUsePrams() ? string.Format(NullFormatProvider.FormatProvider, message, prams) : message).TrimEnd();
                textStream.WriteLine(
                        $"[{level.GetShortCode()} - {Name}]: {message}");
            });
            return;
        }

        var m = MakeFormattedMessage(level, ref message, ref prams);
        if (m.Paragraph.Length > 0)
        {
            if (!m.LastAddWasNewChars)
            {
                m.Paragraph.Append(Environment.NewLine);
            }
            _ = Queuer.WaitForObject(() => AnsiConsole.Write(m.Paragraph));
        }
    }

#if NET6_0_OR_GREATER
    public void Log(Level level, [InterpolatedStringHandlerArgument("", "level")] LogInterpolatedStringHandler builder)
    {
        if (!builder.IsValid)
        {
            return;
        }

        //If the output is redirected then we would of used a StringStringHandler
        if (System.Console.IsOutputRedirected)
        {
            var message = builder.GetHandler<StringStringHandler>()?.GetStringAndClear();
            _ = Queuer.WaitForObject(() =>
            {
                var textStream = level == Level.Error ? System.Console.Error : System.Console.Out;
                textStream.WriteLine(message);
            });
            return;
        }
        
        var para = builder.GetHandler<ConsoleStringHandler>();
        if (para?.Paragraph.Length > 0)
        {
            if (!para.LastAddWasNewChars)
            {
                para.Paragraph.Append(Environment.NewLine);
            }
            _ = Queuer.WaitForObject(() => AnsiConsole.Write(para.Paragraph));
        }
    }
#endif

    private void AddTitle(Paragraph paragraph, Level level)
    {
        var colour = GetTitleColour(level);
        var titleStyle = TitleStyle.Foreground(colour);
        var titleNameStyle = TitleNameStyle.Foreground(colour);
        
        paragraph.Append($"[{level.GetShortCode()} - ", titleStyle);
        paragraph.Append(Name, titleNameStyle);
        paragraph.Append("]: ", titleStyle);
    }
    
    private static Color GetTitleColour(Level level) =>
        level switch
        {
            Level.Error => ErrorColour,
            Level.Warn => WarnColour,
            Level.Trace => TraceColour,
            Level.Info => InfoColour,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };

    private ConsoleStringHandler MakeFormattedMessage(Level level, ref string message, ref object?[]? propertyValues)
    {
        var para = new Paragraph();
        AddTitle(para, level);
        
        var handler = new ConsoleStringHandler(para);
        var messageSpan = message.AsSpan();
        if (messageSpan.IsWhiteSpace())
        {
            handler.AppendLiteral("No message");
            return handler;
        }

        /*If this message needs no formatting then just add it into the string handler*/
        if (messageSpan.IndexOf('{') == -1 || messageSpan.IndexOf('}') == -1)
        {
            handler.AppendLiteral(message + Environment.NewLine);
            return handler;
        }

        while (messageSpan.Length != 0)
        {
            var startBracketInt = messageSpan.IndexOf('{');
            var endBracketInt = messageSpan.IndexOf('}');
            /*This shows that we are at the end of the message
             or the message has no properties to show*/
            if (startBracketInt == -1 && endBracketInt == -1)
            {
                handler.AppendLiteral(messageSpan.ToString());
                return handler;
            }

            handler.AppendLiteral(messageSpan[..startBracketInt].ToString());
#if NETSTANDARD2_1_OR_GREATER 
            if (!int.TryParse(messageSpan[(startBracketInt + 1)..endBracketInt], out var number))
#else
            if (!int.TryParse(messageSpan[(startBracketInt + 1)..endBracketInt].ToString(), out var number))
#endif
            {
                throw new FormatException();
            }

            var prop = propertyValues?[number];
            handler.AppendFormatted(prop, prop?.GetType() ?? typeof(object));
            messageSpan = messageSpan[(endBracketInt + 1)..];
        }

        return handler;
    }

    public void Dispose()
    {
        Queuer.Dispose();
    }
}

/// <summary>
/// Builder to create <see cref="ConsoleLogger"/>
/// </summary>
public sealed class ConsoleLoggerBuilder : LogBuilder
{
    /// <inheritdoc cref="LogBuilder.CreateLogger"/>
    public override ILogger CreateLogger(string name) => new ConsoleLogger(name);
}