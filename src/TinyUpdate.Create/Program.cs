﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TinyUpdate.Core.Update;
using System.Collections.Generic;
using System.Threading.Tasks;
using NuGet.Configuration;
using TinyUpdate.Core.Logging;

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
        private static string _oldVersionLocation = "";

        private static async Task Main(string[] args)
        {
            LoggingCreator.AddLogBuilder(new CustomLoggerBuilder());
            
            //Get what kind of update we are doing
            ShowHeader();
            GetUpdateType();
            var creator = GetUpdateCreator();
            if (creator == null)
            {
                Logging.Error("Unable to create update creator, can't continue....");
                return;
            }
            //TODO: Have a output location op?
            //TODO: Have a verify opt for updates (that they do update and bit-by-bit the same)?

            //Create the updates
            if (_createFullUpdate)
            {
                await CreateFullUpdate(creator);
            }
            if (_createDeltaUpdate)
            {
                await CreateDeltaUpdate(creator);
            }
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

        private static async Task CreateDeltaUpdate(IUpdateCreator updateCreator)
        {
            Logging.WriteLine("Creating Delta update");
            using var progressBar = new ProgressBar();
            var updateCreated = 
                await updateCreator.CreateDeltaPackage(_newVersionLocation, _oldVersionLocation, progress => progressBar.Report((double)progress));
            Logging.Write(updateCreated ? "Success ✓" : "Failed ✖");
        }

        private static async Task CreateFullUpdate(IUpdateCreator updateCreator)
        {
            Logging.WriteLine("Creating Full update");
            using var progressBar = new ProgressBar();
            var updateCreated = 
                await updateCreator.CreateFullPackage(_newVersionLocation, progress => progressBar.Report((double)progress));
            Logging.Write(updateCreated ? " Success ✓" : " Failed ✖");
        }

        private static int RequestNumber(int min, int max)
        {
            int number;
            while (true)
            {
                if (!int.TryParse(Console.ReadLine(), out number))
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
                break;
            }

            return number;
        }
        
        private static void GetUpdateType()
        {
            _createDeltaUpdate = true;
            _createFullUpdate = true;
            _oldVersionLocation = @"C:\Users\aaron\AppData\Local\osulazer\app-2021.129.0";
            _newVersionLocation = @"C:\Users\aaron\AppData\Local\osulazer\app-2021.302.0";
            //return;
            
            Logging.WriteLine("What kind of update do you want to create?");
            Logging.WriteLine("1) Full update");
            Logging.WriteLine("2) Delta update");
            Logging.WriteLine("3) Both");

            //Grab the kind of update they want to do
            var selectedUpdate = RequestNumber(1, 3);

            //Get folders if needed
            _createDeltaUpdate = selectedUpdate != 1;
            _createFullUpdate = selectedUpdate != 2;
            if (_createFullUpdate)
            {
                _newVersionLocation = RequestFolder($"Type in where the{(_createDeltaUpdate ? " new version of the" : "")} application is: ");
            }
            if (_createDeltaUpdate)
            {
                _oldVersionLocation = RequestFolder("Type in where the old version of the application is: ");
            }
        }

        private static string RequestFolder(string message)
        {
            string folder;
            while (true)
            {
                Logging.Write(message);
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    Logging.Error("You need to put something in!!!");
                    continue;
                }
                
                if (!Directory.Exists(line))
                {
                    Logging.Error("Directory doesn't exist");
                    continue;
                }

                folder = line;
                break;
            }
            return folder;
        }
        
        //TODO: Make it filter what OS this creator is made for
        private static IUpdateCreator? GetUpdateCreator()
        {
            //Get any creators that we have in the program we are going to update
            //TODO: Make this use new application folder, for now this works....
            Logging.WriteLine("Finding update creators...");
            var creators = GetAssembliesWithCreators(@"C:\Users\aaron\source\TinyUpdate\src\TinyUpdate.Binary\bin\Debug\netstandard2.1");
            if (!(creators?.Any() ?? false))
            {
                return null;
            }

            //Show any updaters that we have found
            var counter = 0;
            foreach (var (creatorAssembly, creatorTypes) in creators)
            {
                //Shows the assembly that contains a update creator(s)
                var creatorMessage = $"Creator{(creatorTypes.Count > 1 ? "s" : "")} found in {{0}}";
                Logging.WriteLine(creatorMessage, creatorAssembly.GetName().Name);
                Logging.WriteLine(new string('=', creatorMessage.Length));

                /*Show the creator types with the number that will be
                  used to select if they got multiple creators*/
                foreach (var creatorType in creatorTypes)
                {
                    counter++;
                    Logging.WriteLine($"{counter}) {creatorType.FullName}");
                }
                Logging.WriteLine("");
            }

            //Auto select the first creator if we only got one anyway
            Type? ty = null;
            if (creators.Values.Select(x => x.Count).Sum(x => x) == 1)
            {
                ty = creators.Values.First()[0];
            }
            else
            {
                Logging.WriteLine("Select the creator that you want to use (1 - {0})", counter);
            }
            var selectedInt = RequestNumber(1, counter);

            //Loop though all the updaters we got 
            foreach (var creatorTypes in creators.Values)
            {
                //If this is the case then the creator they want isn't from this assembly
                if (creatorTypes.Count < selectedInt)
                {
                    selectedInt -= creatorTypes.Count;
                    continue;
                }

                //Grab the creator type
                ty = creatorTypes[selectedInt - 1];
                break;
            }

            //Create the update creator!
            return Activator.CreateInstance(ty) as IUpdateCreator;
        }
        
        //TODO: Cleanup this because this is *bad*
        /// <summary>
        /// This goes through every assembly and looks for any that contains a <see cref="IUpdateCreator"/> that we can use
        /// </summary>
        /// <param name="applicationLocation">Where the application is stored</param>
        private static Dictionary<Assembly, List<Type>>? GetAssembliesWithCreators(string applicationLocation)
        {
            var creators = new Dictionary<Assembly, List<Type>>();
            foreach (var file in Directory.EnumerateFiles(applicationLocation, "*.dll"))
            {
                var settings = Settings.LoadDefaultSettings(null);
                var nugetFolder = SettingsUtility.GetGlobalPackagesFolder(settings);
                
                //Load the assembly and look at every type that it contains
                var coreAssembly = Assembly.GetAssembly(typeof(IUpdateCreator))?.GetName().Name;
                if (string.IsNullOrWhiteSpace(coreAssembly))
                {
                    Logging.Error("Couldn't get core assembly, able to get creators from other assemblies");
                    return null;
                }
                
                var assemLoader = new SharedAssemblyLoadContext(new [] { coreAssembly }, new []{ applicationLocation, nugetFolder }, AssemblyName.GetAssemblyName(file).Name);
                var assem = assemLoader.LoadFromAssemblyName(AssemblyName.GetAssemblyName(file));

                foreach (var im in assem.DefinedTypes)
                {
                    //See if that type contains the IUpdateCreator interface
                    if (im.ImplementedInterfaces.Any(x => x.FullName == typeof(IUpdateCreator).FullName))
                    {
                        //Add the IUpdateCreator to the list from this assembly 
                        if (!creators.ContainsKey(assem))
                        {
                            creators.Add(assem, new List<Type>());
                        }
                        creators[assem].Add(im);
                    }
                }
            }

            return creators;
        }
    }
}