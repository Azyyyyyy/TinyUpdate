using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging;

//Big thanks for Andrew Larsson showing their loader for shared assemblies!: https://gist.github.com/andrewLarsson/f5351a7c9234ba8c0981037f79108344
namespace TinyUpdate.Create.AssemblyHelper
{
    /*
Copyright 2018 Andrew Larsson
Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute,
sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial
portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
    public class SharedAssemblyLoadContext : AssemblyLoadContext
    {
        private static readonly Regex SystemRegex = new Regex("System*", RegexOptions.Compiled);

        private readonly List<string> _sharedAssemblies;
        private readonly List<string> _assemblyProbingDirectories = new();

        private readonly ILogging _logging = LoggingCreator.CreateLogger(nameof(SharedAssemblyLoadContext));

        private readonly string _mainLibName;
        private string _mainLibFramework = null!;
        private Version _mainLibVersion = null!;

        public SharedAssemblyLoadContext(
            IEnumerable<string> sharedAssemblies,
            IEnumerable<string> assemblyProbingDirectories,
            string mainLibName)
        {
            _mainLibName = mainLibName;

            _sharedAssemblies = sharedAssemblies.ToList();
            _assemblyProbingDirectories.AddRange(assemblyProbingDirectories);
            _assemblyProbingDirectories.Add(Directory.GetCurrentDirectory());
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            //If it's an assembly that we want to load from default
            if (assemblyName.Name != null &&
                _sharedAssemblies.Contains(assemblyName.Name))
            {
                return Default.LoadFromAssemblyName(assemblyName);
            }

            return LoadFrom(assemblyName)!;
        }

        private Assembly? LoadFrom(AssemblyName assemblyName)
        {
            if (assemblyName.Name != null && assemblyName.Version != null)
            {
                return LoadFrom(assemblyName.Name, assemblyName.Version);
            }

            return null;
        }

        private bool LoadAssembly(string? dir, string assemblyName, Version version, out Assembly? assembly)
        {
            assembly = null;
            if (string.IsNullOrWhiteSpace(dir))
            {
                return false;
            }

            //Check that the assembly exists here
            string assemblyPath = Path.Combine(dir, assemblyName) + ".dll";
            if (!File.Exists(assemblyPath))
            {
                return false;
            }

            try
            {
                //Check that the version is what we are expecting
                if (GetAssemblyName(assemblyPath).Version != version)
                {
                    return false;
                }

                //Try to load it in
                assembly = LoadFromAssemblyPath(assemblyPath);
                if (assembly == null)
                {
                    return false;
                }

                //See if we are loading in the main library, if so then get the information about it 
                if (assemblyName == _mainLibName)
                {
                    //This is so we can get what the application was compiled on, not what this application was compiled on
                    var libFrameworkInfo = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
                    var versionIndex = libFrameworkInfo?.IndexOf("=v");
                    if (versionIndex.HasValue && !string.IsNullOrWhiteSpace(libFrameworkInfo))
                    {
                        _mainLibFramework = libFrameworkInfo[..(versionIndex.Value - 8)];
                        _mainLibVersion = Version.Parse(libFrameworkInfo[(versionIndex.Value + 2)..]);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                _logging.Error(e);
            }

            assembly = null;
            return false;
        }

        /// <summary>
        /// Grabs the folders that the other libs should be in
        /// </summary>
        private string[]? GetFolders()
        {
            //We haven't loaded the main lib yet, can't do this...
            if (string.IsNullOrWhiteSpace(_mainLibFramework))
            {
                return null;
            }

            /*There is four folders that the dll will be in
             netXX - .NET Framework
             netstandardX.X - .NET Standard
             netX.X - .NET
             netcoreappX.X .NET Core */

            return _mainLibFramework switch
            {
                //.NET Framework lib
                ".NETFramework" => GetFrameworkFolders(_mainLibVersion),

                //.NET Standard lib
                ".NETStandard" => GetStandardFolders(_mainLibVersion),

                //.NET lib
                ".NETCoreApp" when _mainLibVersion.Major >= 5 => GetNetFolders(_mainLibVersion),

                //.NET Core lib
                ".NET" => GetCoreFolders(_mainLibVersion),
                _ => throw new NotSupportedException("We don't know/support this version of .NET")
            };
        }

        private static string[] GetStandardFolders(Version version)
        {
            //.NET Standard stopped at 2.1 as they moved to just working on .NET
            var folders = new Dictionary<string, Version>
            {
                {"netstandard2.0", new Version(2, 0)},
                {"netstandard2.1", new Version(2, 1)},
            };
            return SelectFolders(folders, version);
        }

        private static string[] GetFrameworkFolders(Version version)
        {
            //.NET Core stopped at 4.8 as they moved to just working on .NET
            var folders = new Dictionary<string, Version>
            {
                {"net46", new Version(4, 6)},
                {"net47", new Version(4, 7)},
                {"net48", new Version(4, 8)},
            };
            return SelectFolders(folders, version);
        }

        private static string[] GetCoreFolders(Version version)
        {
            //.NET Core stopped at 3.1 as they moved to just .NET for naming
            var folders = new Dictionary<string, Version>
            {
                {"netcoreapp2.0", new Version(2, 0)},
                {"netcoreapp2.1", new Version(2, 1)},
                {"netcoreapp2.2", new Version(2, 2)},
                {"netcoreapp3.0", new Version(3, 0)},
                {"netcoreapp3.1", new Version(3, 1)},
            };
            return SelectFolders(folders, version);
        }

        private static string[] SelectFolders(Dictionary<string, Version> folders, Version version)
        {
            var foldersToPass = new List<string>();
            for (int i = 0; folders.Count > i && version >= folders.ElementAt(i).Value; i++)
            {
                foldersToPass.Add(folders.ElementAt(i).Key);
            }

            return foldersToPass.ToArray();
        }

        private static string[] GetNetFolders(Version version)
        {
            //.NET starts at 5.0
            return new[]
            {
                "net5.0",
                $"net{version.Major}.{version.Minor}"
            };
        }

        private Assembly? LoadFrom(string assemblyName, Version assemblyVersion)
        {
            //We don't want to handle loading these in because we already will have them
            if (assemblyName is "netstandard" or "mscorlib")
            {
                return null;
            }

            //If it's a system assembly then we want to load from default if possible
            if (SystemRegex.IsMatch(assemblyName))
            {
                var assemblyIndex = Default.Assemblies.IndexOf(x => x?.GetName().Name == assemblyName);
                if (assemblyIndex != -1)
                {
                    return Default.Assemblies.ElementAt(assemblyIndex);
                }
            }

            foreach (string assemblyProbingDirectoryRoot in _assemblyProbingDirectories)
            {
                //Attempt to load it from the root drive
                if (LoadAssembly(assemblyProbingDirectoryRoot, assemblyName, assemblyVersion, out var assembly))
                {
                    return assembly;
                }

                //We wasn't able to load from root, see if we can find it within a folder we expect
                var fileNetVersions = GetFolders();
                if (fileNetVersions != null)
                {
                    var files = Directory.GetFiles(assemblyProbingDirectoryRoot, $"{assemblyName}.dll",
                        SearchOption.AllDirectories);
                    var filesOrdered = files.Where(x => fileNetVersions.Any(x.Contains))
                        .OrderByDescending(x => x) //Order by folder names, should bring newest versions first
                        .ThenByDescending(x =>
                            fileNetVersions.IndexOf(y =>
                                y?.Contains(x) ?? false)); //Now order by the version folders we expect

                    //Try to load them in
                    if (filesOrdered.Any(file =>
                        LoadAssembly(Path.GetDirectoryName(file), assemblyName, assemblyVersion, out assembly)))
                    {
                        return assembly;
                    }
                }

                //Just try to load *something*
                if (Directory.EnumerateDirectories(assemblyProbingDirectoryRoot, "*", SearchOption.AllDirectories)
                    .Any(assemblyProbingDirectory =>
                        LoadAssembly(assemblyProbingDirectory, assemblyName, assemblyVersion, out assembly)))
                {
                    return assembly;
                }
            }

            return null;
        }
    }
}