using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NuGet.Configuration;
using System.Threading.Tasks;
using TinyUpdate.Core.Update;
using System.Collections.Generic;
using System.Runtime.Loader;
using TinyUpdate.Core;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Create
{
    //TODO: See if more checks can be added
    //TODO: Command line args to skip grabbing information
    internal static class Program
    {
        private static readonly CustomConsoleLogger Logging = new("Tiny Update Creator");
        private static bool _createDeltaUpdate;
        private static string _newVersionLocation = "";
        
        private static bool _createFullUpdate;
        private static string? _oldVersionLocation;

        private static string _outputLocation = "";
        private static Version _applicationNewVersion;
        private static Version? _applicationOldVersion;
        private static string _mainApplicationName = "";

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
            var creator = GetType<IUpdateCreator>("creator");
            if (creator == null)
            {
                Logging.Error("Unable to create update creator, can't continue....");
                return;
            }

            //Get where we are storing the update files created and what is the main .dll
            _outputLocation = RequestFolder($"Where do you want to store the update file{(_createDeltaUpdate && _createFullUpdate ? "s" : "")}?");
            _mainApplicationName = RequestFile("What is the main application file? (.dll file, not what executes the application)", _newVersionLocation);

            GetApplicationMetadata();

            //Create the updates
            if (_createFullUpdate)
            {
                await CreateFullUpdate(creator);
            }
            if (_createDeltaUpdate)
            {
                await CreateDeltaUpdate(creator);
            }

            await VerifyUpdateFiles(creator.Extension);
        }

        private static void GetApplicationMetadata()
        {
            //Try to grab data about the application we are going to creator the update for
            try
            {
                var assemName = Assembly.LoadFile(Path.Combine(_newVersionLocation, _mainApplicationName)).GetName();
                _applicationNewVersion = assemName.Version;
                _mainApplicationName = assemName.Name;
                if (_createDeltaUpdate)
                {
                    _applicationOldVersion = Assembly.LoadFile(Path.Combine(_oldVersionLocation, _mainApplicationName)).GetName().Version;
                }

                Logging.WriteLine("Application Name: {0}", _mainApplicationName);
                Logging.WriteLine("Application new version: {0}", _applicationNewVersion);
                Logging.WriteLine("Application old version: {0}", _applicationOldVersion);
            }
            catch (Exception e)
            {
                Logging.Error(e);
            }

            //Check that they got filled in
            if (_applicationNewVersion == null)
            {
                _applicationNewVersion = RequestVersion("Couldn't get application version, what is the new version of the application");
            }
            if (_createDeltaUpdate && _applicationOldVersion == null)
            {
                _applicationOldVersion = RequestVersion("Couldn't get application version, what is the old version of the application");
            }
            if (string.IsNullOrWhiteSpace(_mainApplicationName))
            {
                _mainApplicationName = RequestString("Couldn't get application name, what is the application name");
            }
            Logging.WriteLine("");
        }
        
        private static async Task VerifyUpdateFiles(string extension)
        {
            //Ask if they want to verify files
            if (!RequestYesOrNo("Do you want us to verify any created updates?", true))
            {
                return;
            }
            Logging.WriteLine("");

            //Grab the applier that we will be using
            var applier = GetType<IUpdateApplier>("applier");
            if (applier == null)
            {
                Logging.Error("Can't get applier, can't verify update...");
                return;
            }
            Logging.WriteLine("Setting up for verifying update files");

            //Get where the old version should be
            Global.ApplicationFolder = Path.Combine(Global.TempFolder, _mainApplicationName);
            Global.ApplicationVersion = _applicationOldVersion;
            var oldVersionLocation = _applicationOldVersion.GetApplicationPath();

            //Delete the old version if it exists, likely here from checking update last time
            if (Directory.Exists(oldVersionLocation))
            {
                Directory.Delete(oldVersionLocation, true);
            }
            Directory.CreateDirectory(oldVersionLocation);

            //Copy the old version files into it's temp folder
            var folderToCopy = _oldVersionLocation ?? _newVersionLocation;
            foreach (var file in Directory.EnumerateFiles(folderToCopy, "*", SearchOption.AllDirectories))
            {
                var fileLocation = Path.Combine(oldVersionLocation, file.Remove(0, folderToCopy.Length + 1));
                var folder = Path.GetDirectoryName(fileLocation);
                Directory.CreateDirectory(folder);
                
                File.Copy(file, fileLocation);
            }

            //Now verify the updates
            var fullFileLocation = GetOutputLocation(false, extension);
            if (_createFullUpdate && File.Exists(fullFileLocation))
            {
                await VerifyUpdate(fullFileLocation, false, _applicationOldVersion, _applicationNewVersion, applier);
                Logging.WriteLine("");
            }
            
            var deltaFileLocation = GetOutputLocation(true, extension);
            if (_createDeltaUpdate && File.Exists(deltaFileLocation))
            {
                await VerifyUpdate(deltaFileLocation, true, _applicationOldVersion, _applicationNewVersion, applier);
            }
        }

        private static async Task<bool> VerifyUpdate(string updateFile, bool isDelta, Version oldVersion, Version newVersion, IUpdateApplier updateApplier)
        {
            //Grab the hash and size of this update file
            Logging.WriteLine("Applying update file {0} to test applying and to be able to cross check files", updateFile);
            var fileStream = File.OpenRead(updateFile);
            var shaHash = SHA256Util.CreateSHA256Hash(fileStream);
            var filesize = fileStream.Length;
            await fileStream.DisposeAsync();

            //Create the release entry and try to do a update
            var entry = new ReleaseEntry(shaHash, Path.GetFileName(updateFile), filesize, isDelta, newVersion,
                Path.GetDirectoryName(updateFile), oldVersion);
            using var applyProgressBar = new ProgressBar();
            var successful = await updateApplier.ApplyUpdate(entry, progress => applyProgressBar.Report((double)progress));
            applyProgressBar.Dispose();
            ShowSuccess(successful);

            //Error out if we wasn't able to apply update
            if (!successful)
            {
                return false;
            }
            
            //Grab files that we have
            var newVersionFiles = Directory.GetFiles(_newVersionLocation,"*", SearchOption.AllDirectories);
            var appliedVersionFiles = Directory.GetFiles(newVersion.GetApplicationPath(),"*", SearchOption.AllDirectories);

            //Check that we got every file that we need/expect
            if (newVersionFiles.LongLength != appliedVersionFiles.LongLength)
            {
                var hasMoreFiles = appliedVersionFiles.LongLength > newVersionFiles.LongLength;
                Logging.Error("There are {0} files in the applied version {1}", 
                    hasMoreFiles ? "more" : "less",
                    hasMoreFiles ? $", files that exist that shouldn't exist:\r\n* {string.Join("\r\n* ", appliedVersionFiles.Except(newVersionFiles))}" : $", files that should exist:\r\n* {string.Join("\r\n* ", newVersionFiles.Except(appliedVersionFiles))}");
                return false;
            }
            
            Logging.WriteLine("Cross checking files");
            double filesCheckedCount = 0;
            var checkProgressBar = new ProgressBar();

            //Check that the files are bit-for-bit and that the folder structure is the same
            foreach (var file in newVersionFiles)
            {
                var ret = file.Remove(0, _newVersionLocation.Length);
                var applIndex = appliedVersionFiles.IndexOf(x => x.EndsWith(ret));

                await using var applStream = File.OpenRead(appliedVersionFiles[applIndex]);
                await using var newVersionStream = File.OpenRead(file);

                //See if the file lengths are the same
                if (applStream.Length != newVersionStream.Length)
                {
                    Logging.Error("File contents of {0} is not the same", ret);
                    return false;
                }

                //Check files bit-for-bit
                while (true)
                {
                    var applBit = applStream.ReadByte();
                    var newVersionBit = newVersionStream.ReadByte();

                    if (applBit != newVersionBit)
                    {
                        Logging.Error("File contents of {0} is not the same", ret);
                        checkProgressBar.Dispose();
                        ShowSuccess(true);
                        return false;
                    }

                    //We hit the end of the file, break out
                    if (applBit == -1)
                    {
                        break;
                    }
                }

                filesCheckedCount++;
                checkProgressBar.Report(filesCheckedCount / newVersionFiles.LongLength);
            }
            checkProgressBar.Dispose();
            ShowSuccess(true);
            
            Logging.WriteLine("No issues with update file and updating");
            return true;
        }
        
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

        private static void ShowSuccess(bool wasSuccessful) => Logging.Write(wasSuccessful ? " Success ✓" : " Failed ✖");
        
        private static async Task CreateDeltaUpdate(IUpdateCreator updateCreator)
        {
            Logging.WriteLine("Creating Delta update");
            var progressBar = new ProgressBar();
            var wasUpdateCreated = 
                await updateCreator.CreateDeltaPackage(
                    _newVersionLocation,
                    _oldVersionLocation,
                    GetOutputLocation(true, updateCreator.Extension),
                    progress => progressBar.Report((double)progress));
            progressBar.Dispose();

            ShowSuccess(wasUpdateCreated);
        }
        
        private static async Task CreateFullUpdate(IUpdateCreator updateCreator)
        {
            Logging.WriteLine("Creating Full update");
            var progressBar = new ProgressBar();
            var wasUpdateCreated = 
                await updateCreator.CreateFullPackage(
                    _newVersionLocation,
                    GetOutputLocation(false, updateCreator.Extension),
                    progress => progressBar.Report((double)progress));
            progressBar.Dispose();

            ShowSuccess(wasUpdateCreated);
        }
        
        private static string GetOutputLocation(bool deltaFile, string extension) => Path.Combine(_outputLocation, $"{_mainApplicationName}.{_applicationNewVersion}-{(deltaFile ? "delta" : "full")}{extension}");

        private static readonly string[] noStrings =
        {
            "no",
            "n"
        };

        private static readonly string[] yesStrings =
        {
            "yes",
            "y"
        };
        
        private static int RequestNumber(int min, int max)
        {
            while (true)
            {
                if (!int.TryParse(Console.ReadLine(), out var number))
                {
                    Logging.Error("You need to give a valid number!!");
                    continue;
                }
                
                //Check that it's not higher then what we have
                if (number < min)
                {
                    Logging.Error("{0} is too low! We need a number in the range of {1} - {2}", number, min, max);
                    continue;
                }

                //Check that it's not higher then what we have
                if (number > max)
                {
                    Logging.Error("{0} is too high! We need a number in the range of {1} - {2}", number, min, max);
                    continue;
                }

                return number;
            }
        }

        private static Version RequestVersion(string message)
        {
            while (true)
            {
                Logging.Write(message + ": ");
                var line = Console.ReadLine();

                //If they put in nothing then error
                if (string.IsNullOrWhiteSpace(line))
                {
                    //They didn't put in something we know, tell them
                    Logging.Error("You need to put in something!!");
                    continue;
                }

                //Give version if we can
                if (Version.TryParse(line, out var version))
                {
                    return version;
                }

                Logging.Error("Can't create a version from {0}", line);
            }
        }
        
        private static string RequestString(string message)
        {
            while (true)
            {
                Logging.Write(message + ": ");
                var line = Console.ReadLine();

                //If they put in nothing then they want the preferred op
                if (!string.IsNullOrWhiteSpace(line))
                {
                    return line;
                }

                //They didn't put in something we know, tell them
                Logging.Error("You need to put in something!!");
            }
        }
        
        private static bool RequestYesOrNo(string message, bool booleanPreferred)
        {
            while (true)
            {
                Logging.WriteLine("");
                Logging.Write(message + (booleanPreferred ? " [Y/n]" : " [N/y]") + ": ");
                var line = Console.ReadLine()?.ToLower();

                //If they put in nothing then they want the preferred op
                if (string.IsNullOrWhiteSpace(line))
                {
                    return booleanPreferred;
                }
                
                //See if what they put in something to show yes or no
                if (yesStrings.Contains(line))
                {
                    return true;
                }
                if (noStrings.Contains(line))
                {
                    return false;
                }
                
                //They didn't put in something we know, tell them
                Logging.Error("You need to put in 'y' for yes or 'n' for no");
            }
        }

        private static string RequestFile(string message, string? folder = null)
        {
            while (true)
            {
                Logging.Write(message + ": ");
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    Logging.Error("You need to put something in!!!");
                    continue;
                }
                line = string.IsNullOrWhiteSpace(folder) ? line : Path.Combine(folder, line);

                if (File.Exists(line))
                {
                    return line;
                }
                Logging.Error("File doesn't exist");
            }
        }
        
        private static string RequestFolder(string message)
        {
            while (true)
            {
                Logging.Write(message + ": ");
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    Logging.Error("You need to put something in!!!");
                    continue;
                }
                
                if (Directory.Exists(line))
                {
                    return line;
                }
                Logging.Error("Directory doesn't exist");
            }
        }

        private static void GetUpdateType()
        {
            Logging.WriteLine("What kind of update do you want to create?");
            Logging.WriteLine("1) Full update");
            Logging.WriteLine("2) Delta update");
            Logging.WriteLine("3) Both");

            //Grab the kind of update they want to do
            var selectedUpdate = RequestNumber(1, 3);

            //Get folders
            _createDeltaUpdate = selectedUpdate != 1;
            _createFullUpdate = selectedUpdate != 2;
            if (_createFullUpdate || _createDeltaUpdate)
            {
                _newVersionLocation = RequestFolder($"Type in where the{(_createDeltaUpdate ? " new version of the" : "")} application is");
            }
            if (_createDeltaUpdate)
            {
                _oldVersionLocation = RequestFolder("Type in where the old version of the application is");
            }
            Logging.WriteLine("");
        }

        //TODO: Make it filter what OS this creator is made for
        private static T? GetType<T>(string frendlyName)
        {
            //Get any the type from any assembly that we know
            //TODO: Make this use new application folder, for now this works just for testing
            Logging.WriteLine($"Finding update {frendlyName}...");
            var creators = GetAssembliesWithType(@"C:\Users\aaron\source\repos\TinyUpdate\src\TinyUpdate.Binary\bin\Debug\netstandard2.1", typeof(T));
            if (!(creators?.Any() ?? false))
            {
                return default;
            }

            //Show any types that we have found
            var counter = 0;
            foreach (var (creatorAssembly, creatorTypes) in creators)
            {
                //Shows the assembly that contains the type
                var creatorMessage = $"{frendlyName}{(creatorTypes.Count > 1 ? "s" : "")} found in {{0}}";
                var creatorAssemblyName = creatorAssembly?.GetName().Name;
                Logging.WriteLine(creatorMessage, creatorAssemblyName);
                Logging.WriteLine(new string('=', creatorMessage.Length + creatorAssemblyName.Length - 3));

                /*Show the types with the number that will be
                  used to select if they got multiple types to choose from*/
                foreach (var creatorType in creatorTypes)
                {
                    counter++;
                    Logging.WriteLine($"{counter}) {creatorType.FullName}");
                }
                Logging.WriteLine("");
            }

            //Get the type that they want to use (Auto selecting if we only got one)
            Type? ty = null;
            int selectedInt = 1;
            if (creators.Values.Select(x => x.Count).Sum(x => x) > 1)
            {
                Logging.WriteLine($"Select the {frendlyName} that you want to use (1 - {0})", counter);
                selectedInt = RequestNumber(1, counter);
            }

            //Loop though all the types we got (This will skip if selectedInt is 0, meaning that it was auto selected)
            foreach (var creatorTypes in creators.Values)
            {
                //If this is the case then the type they want isn't from this assembly
                if (creatorTypes.Count < selectedInt)
                {
                    selectedInt -= creatorTypes.Count;
                    continue;
                }

                //Grab the type
                ty = creatorTypes[selectedInt - 1];
                break;
            }

            //Create the type!
            if (Activator.CreateInstance(ty) is T instance)
            {
                return instance;
            }
            return default;
        }

        private static Assembly FindOrCreateAssembly(string[] sharedAssemblies, string[] probingDirectories, string file)
        {
            foreach (var assemblyLoadContext in AssemblyLoadContext.All)
            {
                var knownAssem = assemblyLoadContext.Assemblies.FirstOrDefault(x => x.Location == file);
                if (knownAssem != null)
                {
                    return knownAssem;
                }
            }
            
            var assemLoader = new SharedAssemblyLoadContext(sharedAssemblies, probingDirectories, AssemblyName.GetAssemblyName(file).Name);
            return assemLoader.LoadFromAssemblyName(AssemblyName.GetAssemblyName(file));
        }
        
        /// <summary>
        /// This goes through every assembly and looks for any that contains the type that we are looking for
        /// </summary>
        /// <param name="applicationLocation">Where the application is stored</param>
        /// <param name="typeToCheckFor">Type to look for</param>
        private static Dictionary<Assembly, List<Type>>? GetAssembliesWithType(string applicationLocation, Type typeToCheckFor)
        {
            if (!typeToCheckFor.IsInterface)
            {
                Logging.Error("Type asked for isn't a interface, can't check for it");
                return null;
            }

            //Get what our core assembly is 
            var coreAssembly = Assembly.GetAssembly(typeof(IUpdateCreator))?.GetName().Name;
            if (string.IsNullOrWhiteSpace(coreAssembly))
            {
                Logging.Error($"Couldn't get core assembly, unable to get {typeToCheckFor.Name} from other assemblies");
                return null;
            }
            var sharedAssemblies = new[] { coreAssembly };

            var types = new Dictionary<Assembly, List<Type>>();
            
            //Get where the nuget folder is as we want to grab contents from there in case
            var nugetFolder = SettingsUtility.GetGlobalPackagesFolder(Settings.LoadDefaultSettings(null));
            var probingDirectories = string.IsNullOrWhiteSpace(nugetFolder) ? 
                new []{ applicationLocation } : 
                new []{ applicationLocation, nugetFolder };

            //Now lets try to find the types
            foreach (var file in Directory.EnumerateFiles(applicationLocation, "*.dll"))
            {
                //We don't want to load this again, don't even try to
                if (Path.GetFileName(file) == "TinyUpdate.Core.dll")
                {
                    continue;
                }
                
                //TODO: See if the assembly is already loaded in and grab that inserted
                //Load in the assembly
                var assem = FindOrCreateAssembly(sharedAssemblies, probingDirectories, file);

                //Look at the types this assembly has 
                foreach (var im in assem.DefinedTypes)
                {
                    //See if this type contains the type as a interface
                    if (im.ImplementedInterfaces.All(x => x != typeToCheckFor))
                    {
                        continue;
                    }

                    //Add the type to the list from this assembly 
                    if (!types.ContainsKey(assem))
                    {
                        types.Add(assem, new List<Type>());
                    }
                    types[assem].Add(im);
                }
            }

            return types;
        }
    }
}