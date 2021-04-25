using System;
using System.IO;
using System.Linq;

namespace TinyUpdate.Create.Helper
{
    /// <summary>
    /// Helper to get data from the console in a easy and useful way
    /// </summary>
    public static class ConsoleHelper
    {
        private static readonly CustomConsoleLogger Logger = new(nameof(ConsoleHelper));

        public static string ShowS(int count) => ShowS(count > 1);
        public static string ShowS(bool show) => show ? "s" : "";

        /// <summary>
        /// Creates a string that displays the contents of <see cref="TimeSpan"/>
        /// </summary>
        /// <param name="timeSpan"><see cref="TimeSpan"/> to use</param>
        public static string TimeSpanToString(TimeSpan timeSpan)
        {
            var s = "";
            if (timeSpan.Days > 0)
            {
                s += $"{timeSpan:%d} Day{ShowS(timeSpan.Days)}, ";
            }

            if (timeSpan.Hours > 0)
            {
                s += $"{timeSpan:%h} Hour{ShowS(timeSpan.Hours)}, ";
            }

            if (timeSpan.Minutes > 0)
            {
                s += $"{timeSpan:%m} Minute{ShowS(timeSpan.Minutes)}, ";
            }

            if (timeSpan.Seconds > 0)
            {
                s += $"{timeSpan:%s} Second{ShowS(timeSpan.Seconds)}, ";
            }

            if (timeSpan.Milliseconds > 0)
            {
                s += $"{timeSpan:%fff} Millisecond{ShowS(timeSpan.Milliseconds)}, ";
            }

            var timeCount = s.Count(x => x == ',');
            if (timeCount > 0)
            {
                s = s[..s.LastIndexOf(',')];
                if (timeCount > 1)
                {
                    var i = s.LastIndexOf(',');
                    s = s[..i] + " and" + s[(i + 1)..];
                }
            }

            return s;
        }

        public static void ShowSuccess(bool wasSuccessful) =>
            Logger.Write(wasSuccessful ? " Success ✓" : " Failed ✖");

        public static int RequestNumber(int min, int max)
        {
            while (true)
            {
                if (!int.TryParse(Console.ReadLine(), out var number))
                {
                    Logger.Error("You need to give a valid number!!");
                    continue;
                }

                //Check that it's not higher then what we have
                if (number < min)
                {
                    Logger.Error("{0} is too low! We need a number in the range of {1} - {2}", number, min, max);
                    continue;
                }

                //Check that it's not higher then what we have
                if (number > max)
                {
                    Logger.Error("{0} is too high! We need a number in the range of {1} - {2}", number, min, max);
                    continue;
                }

                return number;
            }
        }

        public static Version RequestVersion(string message)
        {
            while (true)
            {
                Logger.Write(message + ": ");
                var line = Console.ReadLine();

                //If they put in nothing then error
                if (string.IsNullOrWhiteSpace(line))
                {
                    //They didn't put in something we know, tell them
                    Logger.Error("You need to put in something!!");
                    continue;
                }

                //Give version if we can
                if (Version.TryParse(line, out var version))
                {
                    return version;
                }

                Logger.Error("Can't create a version from {0}", line);
            }
        }

        public static string RequestString(string message)
        {
            while (true)
            {
                Logger.Write(message + ": ");
                var line = Console.ReadLine();

                //Give back what they put in
                if (!string.IsNullOrWhiteSpace(line))
                {
                    return line;
                }

                //They didn't put in something, tell them
                Logger.Error("You need to put in something!!");
            }
        }

        private static readonly string[] NoStrings =
        {
            "no",
            "n"
        };

        private static readonly string[] YesStrings =
        {
            "yes",
            "y"
        };

        public static bool RequestYesOrNo(string message, bool booleanPreferred)
        {
            while (true)
            {
                Logger.WriteLine();
                Logger.Write(message + (booleanPreferred ? " [Y/n]" : " [N/y]") + ": ");
                var line = Console.ReadLine()?.ToLower();

                //If they put in nothing then they want the preferred op
                if (string.IsNullOrWhiteSpace(line))
                {
                    return booleanPreferred;
                }

                //See if what they put in something to show yes or no
                if (YesStrings.Contains(line))
                {
                    return true;
                }

                if (NoStrings.Contains(line))
                {
                    return false;
                }

                //They didn't put in something we know, tell them
                Logger.Error("You need to put in 'y' for yes or 'n' for no");
            }
        }

        public static string RequestFile(string message, string? folder = null)
        {
            while (true)
            {
                Logger.Write(message + ": ");
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    Logger.Error("You need to put something in!!!");
                    continue;
                }

                line = string.IsNullOrWhiteSpace(folder) ? line : Path.Combine(folder, line);

                if (File.Exists(line))
                {
                    return line;
                }

                Logger.Error("File doesn't exist");
            }
        }

        public static string RequestFolder(string message)
        {
            while (true)
            {
                Logger.Write(message + ": ");
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    Logger.Error("You need to put something in!!!");
                    continue;
                }

                if (Directory.Exists(line))
                {
                    return line;
                }

                Logger.Error("Directory doesn't exist");
            }
        }
    }
}