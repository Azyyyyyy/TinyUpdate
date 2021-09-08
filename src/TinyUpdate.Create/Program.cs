using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TinyUpdate.Core.Update;
using TinyUpdate.Core.Logging;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Runtime.InteropServices;
using TinyUpdate.Core;
using TinyUpdate.Core.Utils;
using TinyUpdate.Create.AssemblyHelper;
using TinyUpdate.Create.Helper;
using SemVersion;
using TinyUpdate.Core.Extensions;

[assembly: SemanticVersion("0.0.7-testing")]
namespace TinyUpdate.Create
{
    internal static class Program
    {
        private static readonly CustomConsoleLogger Logger = new("TinyUpdate Creator");

        private static async Task<int> Main(string[] args)
        {
            // Create a root command with some options
            var rootCommand = Commands.GetRootCommand();
            rootCommand.Description = Logger.Name;
            rootCommand.Handler = CommandHandler.Create<Commands>(commands => 
            { 
                Global.CreateDeltaUpdate = commands.Delta; 
                Global.CreateFullUpdate = commands.Full; 
                Global.OutputLocation = (commands.OutputLocation?.Exists ?? false ? commands.OutputLocation.FullName : null)!; 
                Global.NewVersionLocation = 
                    (commands.NewVersionLocation?.Exists ?? false ? commands.NewVersionLocation.FullName : null)!; 
                Global.OldVersionLocation = 
                    commands.OldVersionLocation?.Exists ?? false ? commands.OldVersionLocation.FullName : null;
                Global.MainApplicationFile = commands.ApplicationFile; 
                Global.SkipVerify = commands.SkipVerify; 
                Global.AskIfUserWantsToVerify = !commands.ShouldVerify && !commands.SkipVerify; 
                Global.IntendedOs = commands.IntendedOs; 
                Global.StagingPercentage = commands.StagingPercentage; 
                _applierTypeName = commands.ApplierType; 
                _creatorTypeName = commands.CreatorType;
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
                Logger.Error("{0}", (eventArgs.ExceptionObject.ToString() ?? null));
                Logger.Error("Is terminating: {0}", eventArgs.IsTerminating);
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
                    $"Type in where the{(Global.CreateDeltaUpdate ? " new version of the" : string.Empty)} application is");
            }

            if (Global.CreateDeltaUpdate)
            {
                Global.OldVersionLocation ??=
                    ConsoleHelper.RequestFolder("Type in where the old version of the application is");
            }
            Logger.WriteLine();

            //Grab the update creator
            var creator = GetAssembly.GetTypeFromAssembly<IUpdateCreator>(
                "creator", _creatorTypeName, Global.IntendedOs);
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

            if (ConsoleHelper.RequestYesOrNo("Is this update intended for a certain OS?", false))
            {
                Global.IntendedOs = OSPlatform.Create(ConsoleHelper.RequestString("What OS is this intended for?"));
            }

            if (Global.StagingPercentage is not null &&
                ConsoleHelper.RequestYesOrNo("Do you want to limit the amount of people with this update?", false))
            {
                Console.WriteLine("What percentage of users do you want to have this update?");
                Global.StagingPercentage = ConsoleHelper.RequestNumber(0, 100);
                Console.WriteLine("When you want to change the amount of users with this update, go into the RELEASE file and change the percentage (Last value in the line that contains this update)");
            }

            //Get metadata about the application we are making update files for
            GetApplicationMetadata();
            Global.ApplicationMetadata.ApplicationVersion = Global.ApplicationOldVersion ?? SemanticVersion.BaseVersion();
            Global.ApplicationMetadata.TempFolder = Path.GetTempPath();

            var folder = Path.Combine(Global.ApplicationMetadata.TempFolder, Global.ApplicationMetadata.ApplicationName);
            Directory.CreateDirectory(folder);
            Global.ApplicationMetadata.ApplicationFolder = folder;

            //Create the updates
            var stopwatch = new Stopwatch();
            var releaseFiles = new List<ReleaseFile>(2);
            if (Global.CreateFullUpdate)
            {
                stopwatch.Start();
                if (!CreateUpdate.CreateFullUpdate(creator))
                {
                    Logger.Error("Wasn't able to create full update, exiting....");
                    return;
                }

                stopwatch.Stop();
                Logger.WriteLine("Creating the full update took {0}",
                    ConsoleHelper.TimeSpanToString(stopwatch.Elapsed));
                stopwatch.Restart();

                if (!AddReleaseFile(ref releaseFiles, creator.Extension, false))
                {
                    Logger.Error("Can't create release file entry...");
                    return;
                }
            }

            if (Global.CreateDeltaUpdate)
            {
                stopwatch.Start();
                if (!CreateUpdate.CreateDeltaUpdate(creator))
                {
                    Logger.Error("Wasn't able to create delta update, exiting....");
                    return;
                }

                stopwatch.Stop();
                Logger.WriteLine("Creating the delta update took {0}",
                    ConsoleHelper.TimeSpanToString(stopwatch.Elapsed));
                
                if (!AddReleaseFile(ref releaseFiles, creator.Extension, true))
                {
                    Logger.Error("Can't create release file entry...");
                    return;
                }
            }

            //TODO: Make it limit how many releases are in the file
            if (!await ReleaseFile.CreateReleaseFile(releaseFiles, Global.OutputLocation))
            {
                Logger.Error("Can't create release file....");
                return;
            }

            //and now verify the update files if the user wants to
            await Verify.VerifyUpdateFiles(creator.Extension, _applierTypeName);
        }

        private static bool AddReleaseFile(ref List<ReleaseFile> releaseFiles, string extension, bool isDelta)
        {
            var outputFile = GetOutputLocation(isDelta, extension);
            var fileStream = StreamUtil.SafeOpenRead(outputFile);
            if (fileStream == null)
            {
                return false;
            }
            var updateFileSHA = SHA256Util.CreateSHA256Hash(fileStream);
            releaseFiles.Add(
                new ReleaseFile(
                    updateFileSHA,
                    Path.GetFileName(outputFile),
                    fileStream.Length,
                    Global.StagingPercentage,
                    isDelta ? Global.ApplicationOldVersion : null));
            return true;
        }

        private static void GetApplicationMetadata()
        {
            using var mainLoader = GetAssembly.MakeAssemblyResolver(Global.NewVersionLocation, out _);
            
            //Grab the assemblyName
            var mainApplicationName = Path.GetFileName(Global.MainApplicationFile);
            var fileLocation = Path.Combine(Global.NewVersionLocation, mainApplicationName);
            var assembly = GetAssembly.IsDotNetAssembly(fileLocation)
                ? mainLoader.LoadFromAssemblyPath(fileLocation)
                : null;

            Global.ApplicationNewVersion = assembly?.GetSemanticVersion()!;
            Global.ApplicationMetadata.ApplicationName = assembly?.GetName().Name!;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (Global.CreateDeltaUpdate && Global.OldVersionLocation != null && Global.ApplicationMetadata.ApplicationName != null)
            {
                using var oldVersionLoader = GetAssembly.MakeAssemblyResolver(Global.OldVersionLocation, out _);

                fileLocation = Path.Combine(Global.OldVersionLocation, mainApplicationName);
                Global.ApplicationOldVersion =
                    (GetAssembly.IsDotNetAssembly(fileLocation)
                        ? oldVersionLoader.LoadFromAssemblyPath(fileLocation)
                        : null)?.GetSemanticVersion();
            }

            //Check that all the information we need got filled in
            Global.ApplicationNewVersion ??=
                ConsoleHelper.RequestVersion(
                    "Couldn't get application version, what is the new version of the application");
            Global.ApplicationMetadata.ApplicationName ??=
                ConsoleHelper.RequestString("Couldn't get application name, what is the application name");
            if (Global.CreateDeltaUpdate && Global.ApplicationOldVersion == null)
            {
                Global.ApplicationOldVersion =
                    ConsoleHelper.RequestVersion(
                        "Couldn't get application version, what is the old version of the application");
            }

            //Show that we got thee information
            Logger.WriteLine("Application Name: {0}", Global.ApplicationMetadata.ApplicationName);
            Logger.WriteLine("Application new version: {0}", Global.ApplicationNewVersion);
            Logger.WriteLine("Application old version: {0}", Global.ApplicationOldVersion);
            Logger.WriteLine();
        }

        public static string GetOutputLocation(bool deltaFile, string extension) =>
            Path.Combine(Global.OutputLocation,
                $"{Global.ApplicationMetadata.ApplicationName}.{Global.ApplicationNewVersion}-{(deltaFile ? "delta" : "full")}{(Global.IntendedOs != null ? $"-{Global.IntendedOs}" : string.Empty)}{extension}");

        private const string TinyUpdateText = @"
  _____  _                _   _             _         _        
 |_   _|(_) _ __   _   _ | | | | _ __    __| |  __ _ | |_  ___ 
   | |  | || '_ \ | | | || | | || '_ \  / _` | / _` || __|/ _ \
   | |  | || | | || |_| || |_| || |_) || (_| || (_| || |_|  __/
   |_|  |_||_| |_| \__, | \___/ | .__/  \__,_| \__,_| \__|\___|
                   |___/        |_|                            

";
        private static void ShowHeader()
        {
            var oldColour = Console.ForegroundColor;
            var rnd = new Random();
            foreach (var line in TinyUpdateText.Split('\n'))
            {
                Console.ForegroundColor = (ConsoleColor) rnd.Next(16);
                Logger.WriteLine(line);
            }

            Console.ForegroundColor = oldColour;
            Logger.WriteLine();
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