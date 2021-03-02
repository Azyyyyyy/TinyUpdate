using System;

namespace TinyUpdate.Core.Logger
{
    //TODO: Work on logging and expand how we grab, for now this works
    public static class Logging
    {
        public static ILogging CreateLogger(string name) => new ConsoleLogger(name);
    }

    public class ConsoleLogger : ILogging
    {
        public ConsoleLogger(string name)
        {
            Name = name;
        }

        public string Name { get; }

        private void Write(string type, ConsoleColor console, string message, params object[] propertyValues)
        {
            var oldColour = Console.ForegroundColor;
            Console.ForegroundColor = console;
            Console.Write($"[{type} - {Name}]: ");

            Console.ForegroundColor = oldColour;
            Console.WriteLine(message, propertyValues);
        }

        public void Debug(string message, params object[] propertyValues)
        {
            Write("DEBUG", ConsoleColor.Blue, message, propertyValues);
        }

        public void Error(string message, params object[] propertyValues)
        {
            Write("ERROR", ConsoleColor.Red, message, propertyValues);
        }

        public void Error(Exception e, params object[] propertyValues)
        {
            Error(e.Message, propertyValues);
        }

        public void Information(string message, params object[] propertyValues)
        {
            Write("INFO", ConsoleColor.Cyan, message, propertyValues);
        }

        public void Warning(string message, params object[] propertyValues)
        {
            Write("WARNING", ConsoleColor.Yellow, message, propertyValues);
        }
    }
}
