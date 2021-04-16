using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using NuGet.Configuration;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Create
{
    public static class GetAssembly
    {
        private static readonly CustomConsoleLogger Logger = new(nameof(GetAssembly));

        private static void ShowAssemblyMetadata(Assembly creatorAssembly, List<Type> creatorTypes, string friendlyName, ref int counter)
        {
            if (creatorAssembly == null)
            {
                Logger.Warning("Don't have assembly, skipping...");
                return;
            }
                
            //Shows the assembly that contains the type
            var creatorMessage = $"{friendlyName}{(creatorTypes.Count > 1 ? "s" : "")} found in {{0}}";
            var creatorAssemblyName = creatorAssembly.GetName().Name;
            if (string.IsNullOrWhiteSpace(creatorAssemblyName))
            {
                Logger.Warning("Don't have assembly name, skipping...");
                return;
            }
                
            Logger.WriteLine(creatorMessage, creatorAssemblyName);
            Logger.WriteLine(new string('=', creatorMessage.Length + creatorAssemblyName.Length - 3));

            /*Show the types with the number that will be
              used to select if they got multiple types to choose from*/
            foreach (var creatorType in creatorTypes)
            {
                counter++;
                Logger.WriteLine($"{counter}) {creatorType.FullName}");
            }
            Logger.WriteLine("");
        }
        
        //TODO: Make it filter what OS this creator is made for
        public static T? GetTypeFromAssembly<T>(string friendlyName)
        {
            Logger.WriteLine($"Finding update {friendlyName}...");

            //Get any the type from any assembly that we know
            //TODO: Make this use new application folder, for now this works just for testing
            var creators = GetAssembliesWithType(@"C:\Users\aaron\source\repos\TinyUpdate\src\TinyUpdate.Binary\bin\Debug\netstandard2.1", typeof(T));
            if (!creators.Any())
            {
                return default;
            }

            //Show any types that we have found
            var counter = 0;
            foreach (var (creatorAssembly, creatorTypes) in creators)
            {
                ShowAssemblyMetadata(creatorAssembly, creatorTypes, friendlyName, ref counter);
            }

            //Get the type that they want to use (Auto selecting if we only got one)
            int selectedInt = 1;
            if (creators.Values.Select(x => x.Count).Sum(x => x) > 1)
            {
                Logger.WriteLine($"Select the {friendlyName} that you want to use (1 - {0})", counter);
                selectedInt = Console.RequestNumber(1, counter);
            }

            Type? ty = null;
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
            if (ty != null && Activator.CreateInstance(ty) is T instance)
            {
                return instance;
            }
            return default;
        }

        private static Assembly? FindOrCreateAssembly(string[] sharedAssemblies, string[] probingDirectories, string file)
        {
            //See if we already have the assembly loaded in
            foreach (var assemblyLoadContext in AssemblyLoadContext.All)
            {
                var knownAssembly = assemblyLoadContext.Assemblies.FirstOrDefault(x => x.Location == file);
                if (knownAssembly != null)
                {
                    return knownAssembly;
                }
            }

            var assemblyName = AssemblyName.GetAssemblyName(file).Name;
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                Logger.Error("Can't get assembly Name from {0}", file);
                return null;
            }

            var assemblyLoader = new SharedAssemblyLoadContext(sharedAssemblies, probingDirectories, assemblyName);
            return assemblyLoader.LoadFromAssemblyName(AssemblyName.GetAssemblyName(file));
        }
        
        /// <summary>
        /// This goes through every assembly and looks for any that contains the type that we are looking for
        /// </summary>
        /// <param name="applicationLocation">Where the application is stored</param>
        /// <param name="typeToCheckFor">Type to look for</param>
        private static Dictionary<Assembly, List<Type>> GetAssembliesWithType(string applicationLocation, Type typeToCheckFor)
        {
            var types = new Dictionary<Assembly, List<Type>>();
            if (!typeToCheckFor.IsInterface)
            {
                Logger.Error("Type asked for isn't a interface, can't check for it");
                return types;
            }

            //Get what our core assembly is 
            var coreAssembly = Assembly.GetAssembly(typeof(IUpdateCreator))?.GetName().Name;
            if (string.IsNullOrWhiteSpace(coreAssembly))
            {
                Logger.Error($"Couldn't get core assembly, unable to get {typeToCheckFor.Name} from other assemblies");
                return types;
            }
            var sharedAssemblies = new[] { coreAssembly };
            
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
                
                var assembly = FindOrCreateAssembly(sharedAssemblies, probingDirectories, file);
                if (assembly == null)
                {
                    continue;
                }

                //Look at the types this assembly has 
                foreach (var im in assembly.DefinedTypes)
                {
                    //See if this type contains the type as a interface
                    if (im.ImplementedInterfaces.All(x => x != typeToCheckFor))
                    {
                        continue;
                    }

                    //Add the type to the list from this assembly 
                    if (!types.ContainsKey(assembly))
                    {
                        types.Add(assembly, new List<Type>());
                    }
                    types[assembly].Add(im);
                }
            }

            return types;
        }
    }
}