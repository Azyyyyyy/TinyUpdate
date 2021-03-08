using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using TinyUpdate.Core.Logging;

//Big thanks for Andrew Larsson showing their loader for shared assemblies!: https://gist.github.com/andrewLarsson/f5351a7c9234ba8c0981037f79108344
namespace TinyUpdate.Create
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
        private readonly ILogging _logging = LoggingCreator.CreateLogger("SharedAssemblyLoadContext");
        private readonly List<string> _sharedAssemblies;
        private readonly List<string> _assemblyProbingDirectories = new();
        private readonly string _mainLibName;

        string libFramework;
        Version libVersion;

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
            if (_sharedAssemblies.Contains(assemblyName.Name))
            {
                return Default.LoadFromAssemblyName(assemblyName);
            }
            return LoadFrom(assemblyName);
        }

        public Assembly LoadFrom(AssemblyName assemblyName)
        {
            return LoadFrom(assemblyName.Name, assemblyName.Version);
        }

        private bool LoadAssem(string dir, string assem, Version version, out Assembly assembly)
        {
            assembly = null;
            string assemblyPath = Path.Combine(dir, assem) + ".dll";
            if (!File.Exists(assemblyPath))
            {
                return false;
            }

            try
            {
                if (GetAssemblyName(assemblyPath).Version != version)
                {
                    return false;
                }
                
                if ((assembly = LoadFromAssemblyPath(assemblyPath)) != null && assem == _mainLibName)
                {
                    //This is so we can get what the application was compiled on, not what this application was compiled on
                    var libFrameworkInfo = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
                    var versionIndex = libFrameworkInfo?.IndexOf("=v");
                    if (versionIndex.HasValue && !string.IsNullOrWhiteSpace(libFrameworkInfo))
                    {
                        libFramework = libFrameworkInfo.Substring(0, versionIndex.Value - 8);
                        libVersion = Version.Parse(libFrameworkInfo.Substring(versionIndex.Value + 2));
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
        
        private string[]? GetFolders()
        {
            if (string.IsNullOrWhiteSpace(libFramework))
            {
                return null;
            }
            
            /*There is four folders that the dll will be in
             netXX - .NET Framework
             netstandardX.X - .NET Standard
             netX.X - .NET 5+ (.NET x.y.z)
             netcoreappX.X .NET Core */

            return libFramework switch
            {
                //.NET Framework
                ".NETFramework" => GetFrameworkFolders(libVersion),

                ".NETStandard" => GetStandardFolders(libVersion),
                
                //.NET
                ".NETCoreApp" when libVersion.Major >= 5 => GetNetFolders(libVersion),
                
                //.NET Core
                ".NET" => GetCoreFolders(libVersion),
                _ => throw new NotSupportedException("We don't know/support this version of .NET")
            };
        }
        
        private static string[] GetStandardFolders(Version version)
        {
            //.NET Standard stopped at 2.1 as they moved to just working on .NET
            var folders = new Dictionary<string, Version>
            {
                { "netstandard2.0", new Version(2, 0) },
                { "netstandard2.1", new Version(2, 1) },
            };
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
        
        private static string[] GetFrameworkFolders(Version version)
        {
            //.NET Core stopped at 4.8 as they moved to just working on .NET
            var folders = new Dictionary<string, Version>
            {
                { "net46", new Version(4, 6) },
                { "net47", new Version(4, 7) },
                { "net48", new Version(4, 8) },
            };
            var foldersToPass = new List<string>();
            for (int i = 0; folders.Count > i && version >= folders.ElementAt(i).Value; i++)
            {
                foldersToPass.Add(folders.ElementAt(i).Key);
            }

            return foldersToPass.ToArray();
        }
        
        private static string[] GetCoreFolders(Version version)
        {
            //.NET Core stopped at 3.1 as they moved to just .NET for naming
            var folders = new Dictionary<string, Version>
            {
                { "netcoreapp2.0", new Version(2, 0) },
                { "netcoreapp2.1", new Version(2, 1) },
                { "netcoreapp2.2", new Version(2, 2) },
                { "netcoreapp3.0", new Version(3, 0) },
                { "netcoreapp3.1", new Version(3, 1) },
            };
            var foldersToPass = new List<string>();
            for (int i = 0; folders.Count > i && version >= folders.ElementAt(i).Value; i++)
            {
                foldersToPass.Add(folders.ElementAt(i).Key);
            }

            return foldersToPass.ToArray();
        }

        private Assembly LoadFrom(string assembly, Version assemblyVersion)
        {
            if (assembly == "netstandard" || assembly == "mscorlib" || Regex.IsMatch(assembly, "System*"))
            {
                return null;
            }
            
            Assembly? assemblyRe;
            foreach (string assemblyProbingDirectoryRoot in _assemblyProbingDirectories)
            {
                if (LoadAssem(assemblyProbingDirectoryRoot, assembly, assemblyVersion, out assemblyRe))
                {
                    return assemblyRe;
                }

                var fileNetVersions = GetFolders();
                if (fileNetVersions != null)
                {
                    var files = Directory.GetFiles(assemblyProbingDirectoryRoot, $"{assembly}.dll", SearchOption.AllDirectories);
                    var filesCheck = files.Where(x => fileNetVersions.Any(x.Contains)).OrderByDescending(x => x).ThenByDescending(x => fileNetVersions.IndexOf(y => y.Contains(x)));
                    foreach (var file in filesCheck)
                    {
                        if (LoadAssem(Path.GetDirectoryName(file), assembly, assemblyVersion, out assemblyRe))
                        {
                            return assemblyRe;
                        }
                    }
                }

                if (Directory.EnumerateDirectories(assemblyProbingDirectoryRoot, "*", SearchOption.AllDirectories)
                    .Any(assemblyProbingDirectory => LoadAssem(assemblyProbingDirectory, assembly, assemblyVersion, out assemblyRe)))
                {
                    return assemblyRe;
                }
            }
            return null;
        }
    }
}