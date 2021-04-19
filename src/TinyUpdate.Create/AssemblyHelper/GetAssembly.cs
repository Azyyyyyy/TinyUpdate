using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TinyUpdate.Core.Update;
using TinyUpdate.Create.Helper;

namespace TinyUpdate.Create.AssemblyHelper
{
    public static class GetAssembly
    {
        private static readonly CustomConsoleLogger Logger = new(nameof(GetAssembly));

        //From https://stackoverflow.com/a/15608028
        public static bool IsDotNetAssembly(string file)
        {
            if (!File.Exists(file))
            {
                return false;
            }
            
            using Stream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            using BinaryReader binaryReader = new BinaryReader(fileStream);
            if (fileStream.Length < 64)
            {
                return false;
            }

            //PE Header starts @ 0x3C (60). Its a 4 byte header.
            fileStream.Position = 0x3C;
            var peHeaderPointer = binaryReader.ReadUInt32();
            if (peHeaderPointer == 0)
            {
                peHeaderPointer = 0x80;
            }

            // Ensure there is at least enough room for the following structures:
            //     24 byte PE Signature & Header
            //     28 byte Standard Fields         (24 bytes for PE32+)
            //     68 byte NT Fields               (88 bytes for PE32+)
            // >= 128 byte Data Dictionary Table
            if (peHeaderPointer > fileStream.Length - 256)
            {
                return false;
            }

            // Check the PE signature.  Should equal 'PE\0\0'.
            fileStream.Position = peHeaderPointer;
            uint peHeaderSignature = binaryReader.ReadUInt32();
            if (peHeaderSignature != 0x00004550)
            {
                return false;
            }

            // skip over the PEHeader fields
            fileStream.Position += 20;

            const ushort PE32 = 0x10b;
            const ushort PE32Plus = 0x20b;

            // Read PE magic number from Standard Fields to determine format.
            var peFormat = binaryReader.ReadUInt16();
            if (peFormat != PE32 && peFormat != PE32Plus)
            {
                return false;
            }

            // Read the 15th Data Dictionary RVA field which contains the CLI header RVA.
            // When this is non-zero then the file contains CLI data otherwise not.
            var dataDictionaryStart = (ushort)(peHeaderPointer + (peFormat == PE32 ? 232 : 248));
            fileStream.Position = dataDictionaryStart;

            var cliHeaderRva = binaryReader.ReadUInt32();
            return cliHeaderRva != 0;
        }
        
        private static void ShowAssemblyMetadata(Assembly assembly, IReadOnlyCollection<Type> types, string friendlyName, ref int counter)
        {
            //Shows the assembly that contains the type
            var assemblyName = assembly.GetName().Name;
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                Logger.Warning("Don't have assembly name, skipping...");
                return;
            }
            var message = $"{friendlyName}{ConsoleHelper.ShowS(types.Count)} found in {{0}}";
                
            Logger.WriteLine(message, assemblyName);
            Logger.WriteLine(new string('=', message.Length + assemblyName.Length - 3));

            /*Show the types with the number that will be
              used to select if they got multiple types to choose from*/
            foreach (var type in types)
            {
                counter++;
                Logger.WriteLine($"{counter}) {type.FullName}");
            }
            Logger.WriteLine("");
        }
        
        //TODO: Make it filter based on what OS we plan to make the update for
        public static T? GetTypeFromAssembly<T>(string friendlyName, string? automaticallyLoad = null)
        {
            Logger.WriteLine($"Finding update {friendlyName}...");

            //Get any the type from any assembly that we know
            var availableTypes = GetAssembliesWithType(Global.NewVersionLocation, typeof(T));
            if (!availableTypes.Any())
            {
                return default;
            }
            Type? ty = null;

            //Show any types that we have found
            var counter = 0;
            foreach (var (assembly, types) in availableTypes)
            {
                ShowAssemblyMetadata(assembly, types, friendlyName, ref counter);
                ty ??= types.FirstOrDefault(type => type.Name == automaticallyLoad);
            }
            
            //Get the type that they want to use (Auto selecting if we only got one)
            int selectedInt = 1;
            if (ty == null && availableTypes.Values.Select(x => x.Count).Sum(x => x) > 1)
            {
                Logger.WriteLine($"Select the {friendlyName} that you want to use (1 - {0})", counter);
                selectedInt = ConsoleHelper.RequestNumber(1, counter);
            }

            //Loop though all the types we got (This will skip if selectedInt is 0, meaning that it was auto selected)
            foreach (var types in availableTypes.Values)
            {
                if (ty != null)
                {
                    break;
                }
                
                //If this is the case then the type they want isn't from this assembly
                if (types.Count < selectedInt)
                {
                    selectedInt -= types.Count;
                    continue;
                }

                //Grab the type
                ty = types[selectedInt - 1];
                break;
            }

            //Create the type!
            if (ty != null && Activator.CreateInstance(ty) is T instance)
            {
                return instance;
            }
            return default;
        }

        private static bool CanLoadFile(string file)
        {
            string[] blacklistedFiles =
            {
                "TinyUpdate.Core.dll",
                "mscorlib.dll",
                "ucrtbase.dll",
                "netstandard.dll",
                "System.Private.CoreLib.dll"
            };

            //Don't try to load the file if it's any of these files or not an PE file
            var fileName = Path.GetFileName(file);
            if (blacklistedFiles.Contains(fileName)
                || Regex.IsMatch(fileName, "api-ms*")
                || !IsDotNetAssembly(file))
            {
                return false;
            }
            
            //assembly manifest
            var assemblyName = GetAssemblyName(file);
            if (string.IsNullOrWhiteSpace(assemblyName?.Name))
            {
                Logger.Error("Can't get assembly name from {0}", file);
                return false;
            }

            return true;
        }

        private static AssemblyName? GetAssemblyName(string file) => 
            IsDotNetAssembly(file) ? 
                AssemblyName.GetAssemblyName(file) 
                : null;

        private static Type? SafeGetInterface(this Type type, string name)
        {
            try
            {
                return type.GetInterface(name);
            }
            catch (Exception)
            {
                // ignored
            }

            return null;
        }

        /*When we run this the first time we put any files that contained
         something useful and then re-look at them files, saves time rechecking*/
        private static List<string> _cachedFiles = new List<string>();
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

            // Create PathAssemblyResolver that can resolve assemblies using the created list.
            var resolver = new PathAssemblyResolver(Directory.EnumerateFiles(applicationLocation, "*.dll"));
            using var mlc = new MetadataLoadContext(resolver);

            //Now lets try to find the types
            foreach (var file in 
                _cachedFiles.Any() ?
                    _cachedFiles :
                    Directory.EnumerateFiles(applicationLocation, "*.dll"))
            {
                //Check that it's something we can load in
                if (!CanLoadFile(file))
                {
                    continue;
                }

                Assembly assembly = mlc.LoadFromAssemblyPath(file);

                //Look at the types this assembly has 
                foreach (var typeInfo in assembly.GetTypes())
                {
                    //See if this type contains the type as a interface
                    if (typeInfo.SafeGetInterface(typeToCheckFor.Name) == null)
                    {
                        continue;
                    }

                    if (!_cachedFiles.Contains(file))
                    {
                        _cachedFiles.Add(file);
                    }

                    //Add the type to the list from this assembly 
                    if (!types.ContainsKey(assembly))
                    {
                        types.Add(assembly, new List<Type>());
                    }
                    types[assembly].Add(typeInfo);
                }
            }

            //When we get here we want to load in any that matched, or we can't use it
            var loadedTypes = new Dictionary<Assembly, List<Type>>();
            foreach (var (assembly, assemblyTypes) in types)
            {
                var sharedAssemblies = new[] { coreAssembly };
                var probingDirectories = new [] { applicationLocation };

                var assemblyLoadContext = new SharedAssemblyLoadContext(sharedAssemblies, probingDirectories, assembly.Location);
                var loadedAssembly = assemblyLoadContext.LoadFromAssemblyPath(assembly.Location);
                loadedTypes.Add(loadedAssembly, new List<Type>());
                foreach (var assemblyType in assemblyTypes)
                {
                    loadedTypes[loadedAssembly].Add(
                        loadedAssembly.DefinedTypes.First(x =>
                            x.FullName == assemblyType.FullName));
                }
            }
            
            return loadedTypes;
        }
    }
}