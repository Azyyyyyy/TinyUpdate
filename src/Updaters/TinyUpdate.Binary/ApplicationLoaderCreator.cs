using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using TinyUpdate.Core;

namespace TinyUpdate.Binary
{
    public static class ApplicationLoaderCreator
    {
        //main.cpp: Change {APPLICATIONLOCATION} with location
        //If icon exists: add 'IDI_ICON1 ICON DISCARDABLE "app.ico"' to app.rc
        /// <summary>
        /// Creates the loader that will be needed for loading the application
        /// </summary>
        /// <param name="path">The relative path to the application</param>
        /// <param name="iconLocation">Where the icon is for this application</param>
        /// <param name="outputLocation">Where to put the loader</param>
        /// <returns>If we was able to create the loader</returns>
        public static bool CreateLoader(string path, string? iconLocation, string outputLocation, string applicationName)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //TODO: Log about OS while not added
                return true;
            }
            
            var templateFolder = Path.Combine(Global.TempFolder, applicationName, "Loader Template");
            
            //TODO: Get based on OS
            //Get stream of template (contained in zip)
            var assembly = typeof(ApplicationLoaderCreator).GetTypeInfo().Assembly;
            var zipStream = assembly.GetManifestResourceStream("TinyUpdate.Binary.LoaderTemplate.windows.zip");
            if (Directory.Exists(templateFolder))
            {
                Directory.Delete(templateFolder, true);
            }
            
            //Extract zip
            var templateZip = new ZipArchive(zipStream, ZipArchiveMode.Read);
            templateZip.ExtractToDirectory(templateFolder);
            zipStream.Dispose();
            templateZip.Dispose();
            
            //Drop icon to folder if it exists
            if (!string.IsNullOrWhiteSpace(iconLocation))
            {
                File.Copy(iconLocation, Path.Combine(templateFolder, "app.ico"));
                File.WriteAllText(Path.Combine(templateFolder, "app.rc"), "IDI_ICON1 ICON DISCARDABLE \"app.ico\"");
            }
            
            //Change main.cpp
            var mainFileLocation = Path.Combine(templateFolder, "main.cpp");
            var mainFile = File.ReadAllLines(mainFileLocation);
            var changedContent = false;
            for (int i = 0; i < mainFile.Length; i++)
            {
                if (mainFile[i].Contains("{APPLICATIONLOCATION}"))
                {
                    mainFile[i] = mainFile[i].Replace("{APPLICATIONLOCATION}", path.Replace(@"\", @"\\"));
                    changedContent = true;
                    break;
                }
            }

            if (!changedContent)
            {
                //TODO: Log
                return false;
            }
            File.WriteAllLines(mainFileLocation, mainFile);
            
            //Build
            //TODO: Make it find cmake & VS Build tools
            var toolsFile =
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Preview\VC\Auxiliary\Build\vcvars64.bat";
            var cmakeLocation = @"D:\aaron\Downloads\cmake-3.21.0-rc3-windows-x86_64\bin\cmake.exe";
            var buildFolder = Path.Combine(templateFolder, "cmake-build");

            //TODO: Make this OS dependent
            var cacheBat = Path.Combine(templateFolder, "processCache.bat");
            File.WriteAllLines(cacheBat, new []
            {
                $"@call \"{toolsFile}\"",
                $"\"{cmakeLocation}\" -DCMAKE_BUILD_TYPE=Release -G \"CodeBlocks - NMake Makefiles\" -S \"{templateFolder}\" -B \"{buildFolder}\"",
                $"\"{cmakeLocation}\" --build \"{buildFolder}\" --target all"
            });
            //TODO: Make output be consumed by us and not by the console
            var buildProcess = Process.Start(new ProcessStartInfo("cmd.exe")
            {
                Arguments = $"/c \"{cacheBat}\"",
                UseShellExecute = false
            });

            buildProcess?.WaitForExit();
            if (buildProcess?.ExitCode != 0)
            {
                //TODO: Log
                return false;
            }
            
            var outputFile = Path.Combine(outputLocation, applicationName + ".exe");
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
            File.Move(Path.Combine(buildFolder, "ApplicationLoader.exe"), outputFile);
            return true;
        }
    }
}