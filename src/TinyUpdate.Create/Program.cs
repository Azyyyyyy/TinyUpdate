using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using TinyUpdate.Core.Update;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Create
{
    //TODO: Command line args to skip grabbing information
    internal static class Program
    {
        private static readonly CustomConsoleLogger Logging = new("Tiny Update Creator");

        private static async Task Main(string[] args)
        {
            //Add logging
            LoggingCreator.AddLogBuilder(new CustomLoggerBuilder());
            TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
            {
                Logging.Error("Unhandled task exception has happened");
                Logging.Error(eventArgs.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                Logging.Error("Unhandled exception has happened");
                Logging.Error("{0}", eventArgs.ExceptionObject);
                Logging.Error("Is terminating due to error?: {0}", eventArgs.IsTerminating);
            };
            
            //Get what kind of update we are doing 
            ShowHeader();
            GetUpdateType();

            //Grab the update creator
            var creator = GetAssembly.GetTypeFromAssembly<IUpdateCreator>("creator");
            if (creator == null)
            {
                Logging.Error("Unable to create update creator, can't continue....");
                return;
            }

            //Get where we are storing the update files created and what is the main .dll
            Global.OutputLocation = Console.RequestFolder($"Where do you want to store the update file{(Global.CreateDeltaUpdate && Global.CreateFullUpdate ? "s" : "")}?");
            Global.MainApplicationName = Console.RequestFile("What is the main application file? (.dll file, not what executes the application)", Global.NewVersionLocation);

            //Get metadata about the application we are making update files for
            GetApplicationMetadata();

            //Create the updates
            if (Global.CreateFullUpdate)
            {
                if (!await CreateUpdate.CreateFullUpdate(creator))
                {
                    Logging.Error("Wasn't able to create full update, exiting....");
                    return;
                }
            }
            if (Global.CreateDeltaUpdate)
            {
                if (!await CreateUpdate.CreateDeltaUpdate(creator))
                {
                    Logging.Error("Wasn't able to create delta update, exiting....");
                    return;
                }
            }

            //and now verify the update files if the user wants to
            await Verify.VerifyUpdateFiles(creator.Extension);
        }

        private static void GetApplicationMetadata()
        {
            try
            {
                var assemblyName = Assembly.LoadFile(Path.Combine(Global.NewVersionLocation, Global.MainApplicationName)).GetName();
                Global.ApplicationNewVersion = assemblyName.Version;
                Global.MainApplicationName = assemblyName.Name;
                if (Global.CreateDeltaUpdate && Global.OldVersionLocation != null && Global.MainApplicationName != null)
                {
                    Global.ApplicationOldVersion = Assembly.LoadFile(Path.Combine(Global.OldVersionLocation, Global.MainApplicationName)).GetName().Version;
                }
            }
            catch (Exception e)
            {
                Logging.Error(e);
            }

            //Check that all the information we need got filled in
            Global.ApplicationNewVersion ??= Console.RequestVersion("Couldn't get application version, what is the new version of the application");
            Global.MainApplicationName ??= Console.RequestString("Couldn't get application name, what is the application name");
            if (Global.CreateDeltaUpdate && Global.ApplicationOldVersion == null)
            {
                Global.ApplicationOldVersion = Console.RequestVersion("Couldn't get application version, what is the old version of the application");
            }
            Logging.WriteLine("Application Name: {0}", Global.MainApplicationName);
            Logging.WriteLine("Application new version: {0}", Global.ApplicationNewVersion);
            Logging.WriteLine("Application old version: {0}", Global.ApplicationOldVersion);
            Logging.WriteLine("");
        }
        
        public static string GetOutputLocation(bool deltaFile, string extension) => Path.Combine(Global.OutputLocation, $"{Global.MainApplicationName}.{Global.ApplicationNewVersion}-{(deltaFile ? "delta" : "full")}{extension}");

        private static void ShowHeader()
        {
            Logging.WriteLine(
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
            Logging.WriteLine("What kind of update do you want to create?");
            Logging.WriteLine("1) Full update");
            Logging.WriteLine("2) Delta update");
            Logging.WriteLine("3) Both");

            //Grab the kind of update they want to do
            var selectedUpdate = Console.RequestNumber(1, 3);

            //Get folders
            Global.CreateDeltaUpdate = selectedUpdate != 1;
            Global.CreateFullUpdate = selectedUpdate != 2;
            if (Global.CreateFullUpdate || Global.CreateDeltaUpdate)
            {
                Global.NewVersionLocation = Console.RequestFolder($"Type in where the{(Global.CreateDeltaUpdate ? " new version of the" : "")} application is");
            }
            if (Global.CreateDeltaUpdate)
            {
                Global.OldVersionLocation = Console.RequestFolder("Type in where the old version of the application is");
            }
            Logging.WriteLine("");
        }
    }
}