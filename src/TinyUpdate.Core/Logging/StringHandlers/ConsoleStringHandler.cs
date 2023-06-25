using System;
using System.Collections.Generic;
using Spectre.Console;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging.Loggers.Console;

namespace TinyUpdate.Core.Logging.StringHandlers;

public sealed class ConsoleStringHandler : ILogInterpolatedStringHandler
{
    //Gray colour to use for normal text
    private static readonly Dictionary<Type, object> GetCustomColour = new();
    private static readonly Color Gray = new(209, 209, 209);
    public ConsoleStringHandler(Paragraph? paragraph = null)
    {
        Paragraph = paragraph ?? new Paragraph();
    }

    public Paragraph Paragraph { get; }
    public bool LastAddWasNewChars { get; private set; }

    /// <summary>
    /// Adds a function to get the colour to output when processing <see cref="T"/>
    /// </summary>
    public static void AddCustomColour<T>(Func<T?, Color?> getColour) => GetCustomColour[typeof(T)] = getColour;
    
    public void AppendLiteral(string s)
    {
        /*If it's just new chars then no
          point in adding a style to it*/
        if (s.IsNewChars())
        {
            AddText(s, usePlainStyle: true);
            return;
        }

        AddText(s, Gray);
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
    public void AppendFormatted<T>(T t, Type type) => AddText(t, other: Decoration.Bold, type: type);

    private void AddText<T>(T? item, Color? colour = null, Decoration? other = null, Type? type = null, 
        bool usePlainStyle = false)
    {
        var style = usePlainStyle ? Style.Plain : new Style(
            foreground: colour ?? GetColourBasedOnType(item, type),
            decoration: other);

        var message = item?.ToString() ?? "null";
        LastAddWasNewChars = message.EndsWithNewChars();
        Paragraph.Append(message, style);
    }

    private static Color GetColourBasedOnType<T>(T? o, Type? type)
    {
        if (o == null)
        {
            return Color.Blue;
        }

        /*If we have a colour formatter given to us then use that before checking if the object
         is a IColouredOutput*/
        if (GetCustomColour.TryGetValue(type ?? o.GetType(), out var objAct)
            && objAct is Func<T, Color?> action)
        {
            var colour = action(o);
            if (colour.HasValue)
            {
                return colour.Value;
            }
        }

        //If we are checking the same type and it's got a custom colour output, use it!
        if (type == o.GetType() && o is IColouredOutput { Colour: not null } colouredOutput)
        {
            return colouredOutput.Colour.Value;
        }
        
        type ??= o.GetType();
        if (type == typeof(bool))
            return Color.Blue;
        if (type == typeof(string))
            return Color.Cyan1;
        if (type.IsNumber())
            return Color.Magenta1;

        return Color.Green;
    }
}