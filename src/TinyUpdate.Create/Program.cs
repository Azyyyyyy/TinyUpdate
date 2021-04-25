using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using TinyUpdate.Core.Update;
using TinyUpdate.Core.Logging;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Runtime.InteropServices;
using TinyUpdate.Create.AssemblyHelper;
using TinyUpdate.Create.Helper;

namespace TinyUpdate.Create
{
    internal static class Program
    {
        private static readonly CustomConsoleLogger Logger = new("TinyUpdate Creator");

        private static async Task<int> Main(string[] args)
        {
            // Create a root command with some options
            var rootCommand = new RootCommand
            {
                new Option<bool>(
                    new[] {"-d", "--delta"},
                    "Create a delta update"),
                new Option<bool>(
                    new[] {"-f", "--full"},
                    "Create a full update"),
                new Option<DirectoryInfo?>(
                    new[] {"-o", "--output-location"},
                    "Where any files created should be stored"),
                new Option<DirectoryInfo?>(
                    new[] {"--nl", "--new-version-location"},
                    "Where the new version of the application is stored"),
                new Option<DirectoryInfo?>(
                    new[] {"--ol", "--old-version-location"},
                    "Where the old version of the application is stored"),
                new Option<string>(
                    new[] {"--af", "--application-file"},
                    "What is the main application file?"),
                new Option<bool>(
                    new[] {"-s", "--skip-verifying"},
                    "Skip verifying that the update applies correctly"),
                new Option<bool>(
                    new[] {"-v", "--verify"},
                    "Verify that the update applies correctly"),
                new Option<string?>(
                    new[] {"--at", "--applier-type"},
                    "What type is used for applying the update"),
                new Option<string?>(
                    new[] {"--ct", "--creator-type"},
                    "What type is used for creating updates"),
                new Option<string?>(
                    new[] {"--os", "--intended-os"},
                    "What os the update is intended for"),
            };
            rootCommand.Description = Logger.Name;

            rootCommand.Handler = CommandHandler
                .Create<bool, bool, DirectoryInfo?, DirectoryInfo?, DirectoryInfo?, string, bool, bool, string?, string?
                    , string?>(
                    async (delta, full, outputLocation, newVersionLocation, oldVersionLocation, applicationFile,
                        skipVerifying, verify, applierType, creatorType, intendedOs) =>
                    {
                        Global.CreateDeltaUpdate = delta;
                        Global.CreateFullUpdate = full;
                        Global.OutputLocation = (outputLocation?.Exists ?? false ? outputLocation.FullName : null)!;
                        Global.NewVersionLocation =
                            (newVersionLocation?.Exists ?? false ? newVersionLocation.FullName : null)!;
                        Global.OldVersionLocation =
                            oldVersionLocation?.Exists ?? false ? oldVersionLocation.FullName : null;
                        Global.MainApplicationFile = applicationFile;
                        Global.SkipVerify = skipVerifying;
                        Global.AskIfUserWantsToVerify = !verify && !skipVerifying;
                        Global.IntendedOS = !string.IsNullOrWhiteSpace(intendedOs)
                            ? OSPlatform.Create(intendedOs)
                            : null;
                        _applierTypeName = applierType;
                        _creatorTypeName = creatorType;
                    });
            
            //Parse args, throwing the error if it couldn't
            var commandResult = await rootCommand.InvokeAsync(args);
            if (commandResult != 0)
            {
                return commandResult;
            }
            
            //Add logging
            LoggingCreator.GlobalLevel = LogLevel.Warn;
            LoggingCreator.AddLogBuilder(new CustomLoggerBuilder());
            TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
            {
                Logger.Error("Unhandled task exception has happened");
                Logger.Error(eventArgs.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                Logger.Error("Unhandled exception has happened");
                Logger.Error("{0}", eventArgs.ExceptionObject);
                Logger.Error("Is terminating due to error?: {0}", eventArgs.IsTerminating);
            };
            
            //Run the application if we get this far
            await Run();
            return 0;
        }

        private static string? _applierTypeName;
        private static string? _creatorTypeName;

        private static async Task Run()
        {
            //Show header and get what kind of update we are doing 
            ShowHeader();
            GetUpdateType();

            //Get folders that contain the different versions
            if (Global.CreateFullUpdate || Global.CreateDeltaUpdate)
            {
                // ReSharper disable once ConstantNullCoalescingCondition
                Global.NewVersionLocation ??= ConsoleHelper.RequestFolder(
                    $"Type in where the{(Global.CreateDeltaUpdate ? " new version of the" : "")} application is");
            }

            if (Global.CreateDeltaUpdate)
            {
                Global.OldVersionLocation ??=
                    ConsoleHelper.RequestFolder("Type in where the old version of the application is");
            }

            Logger.WriteLine();

            //Grab the update creator
            var creator =
                GetAssembly.GetTypeFromAssembly<IUpdateCreator>("creator", _creatorTypeName, Global.IntendedOS);
            if (creator == null)
            {
                Logger.Error("Unable to create update creator, can't continue....");
                return;
            }

            //Get where we are storing the update files created and what is the main .dll is
            Global.OutputLocation ??= ConsoleHelper.RequestFolder(
                $"Where do you want to store the update file{ConsoleHelper.ShowS(Global.CreateDeltaUpdate && Global.CreateFullUpdate)}?");
            Global.MainApplicationFile ??=
                ConsoleHelper.RequestFile("What is the main application file?", Global.NewVersionLocation);

            //Get metadata about the application we are making update files for
            GetApplicationMetadata();

            //Create the updates
            var stopwatch = new Stopwatch();
            if (Global.CreateFullUpdate)
            {
                stopwatch.Start();
                if (!await CreateUpdate.CreateFullUpdate(creator))
                {
                    Logger.Error("Wasn't able to create full update, exiting....");
                    return;
                }

                stopwatch.Stop();
                Logger.WriteLine("Creating the full update took {0}",
                    ConsoleHelper.TimeSpanToString(stopwatch.Elapsed));
                stopwatch.Restart();
            }

            if (Global.CreateDeltaUpdate)
            {
                stopwatch.Start();
                if (!await CreateUpdate.CreateDeltaUpdate(creator))
                {
                    Logger.Error("Wasn't able to create delta update, exiting....");
                    return;
                }

                stopwatch.Stop();
                Logger.WriteLine("Creating the delta update took {0}",
                    ConsoleHelper.TimeSpanToString(stopwatch.Elapsed));
            }

            //and now verify the update files if the user wants to
            await Verify.VerifyUpdateFiles(creator.Extension, _applierTypeName);
        }

        private static void GetApplicationMetadata()
        {
            //Grab the assemblyName
            var mainApplicationName = Global.MainApplicationFile;
            var fileLocation = Path.Combine(Global.NewVersionLocation, mainApplicationName);
            var assemblyName = GetAssembly.IsDotNetAssembly(fileLocation)
                ? AssemblyName.GetAssemblyName(fileLocation)
                : null;

            Global.ApplicationNewVersion = assemblyName?.Version;
            Global.MainApplicationName = assemblyName?.Name;
            if (Global.CreateDeltaUpdate && Global.OldVersionLocation != null && Global.MainApplicationName != null)
            {
                fileLocation = Path.Combine(Global.OldVersionLocation, mainApplicationName);
                Global.ApplicationOldVersion =
                    (GetAssembly.IsDotNetAssembly(fileLocation)
                        ? AssemblyName.GetAssemblyName(fileLocation)
                        : null)?.Version;
            }

            //Check that all the information we need got filled in
            Global.ApplicationNewVersion ??=
                ConsoleHelper.RequestVersion(
                    "Couldn't get application version, what is the new version of the application");
            Global.MainApplicationName ??=
                ConsoleHelper.RequestString("Couldn't get application name, what is the application name");
            if (Global.CreateDeltaUpdate && Global.ApplicationOldVersion == null)
            {
                Global.ApplicationOldVersion =
                    ConsoleHelper.RequestVersion(
                        "Couldn't get application version, what is the old version of the application");
            }

            //Show that we got thee information
            Logger.WriteLine("Application Name: {0}", Global.MainApplicationName);
            Logger.WriteLine("Application new version: {0}", Global.ApplicationNewVersion);
            Logger.WriteLine("Application old version: {0}", Global.ApplicationOldVersion);
            Logger.WriteLine();
        }

        public static string GetOutputLocation(bool deltaFile, string extension) =>
            Path.Combine(Global.OutputLocation,
                $"{Global.MainApplicationName}.{Global.ApplicationNewVersion}-{(deltaFile ? "delta" : "full")}{(Global.IntendedOS != null ? $"-{Global.IntendedOS}" : "")}{extension}");

        private static void ShowHeader()
        {
            Logger.WriteLine(
                @"
  _____  _                _   _             _         _        
 |_   _|(_) _ __   _   _ | | | | _ __    __| |  __ _ | |_  ___ 
   | |  | || '_ \ | | | || | | || '_ \  / _` | / _` || __|/ _ \
   | |  | || | | || |_| || |_| || |_) || (_| || (_| || |_|  __/
   |_|  |_||_| |_| \__, | \___/ | .__/  \__,_| \__,_| \__|\___|
                   |___/        |_|                            

");
        }

        private static void GetUpdateType()
        {
            //See if we already got it (From Cli)
            if (Global.CreateDeltaUpdate || Global.CreateFullUpdate)
            {
                return;
            }

            Logger.WriteLine("What kind of update do you want to create?");
            Logger.WriteLine("1) Full update");
            Logger.WriteLine("2) Delta update");
            Logger.WriteLine("3) Both");

            //Grab the kind of update they want to do
            var selectedUpdate = ConsoleHelper.RequestNumber(1, 3);

            //Get what we want to process
            Global.CreateDeltaUpdate = selectedUpdate != 1;
            Global.CreateFullUpdate = selectedUpdate != 2;
        }
    }
}