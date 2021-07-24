using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Binary
{
    public static class ApplicationLoaderCreator
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(ApplicationLoaderCreator));
        private static readonly Assembly Assembly = typeof(ApplicationLoaderCreator).GetTypeInfo().Assembly;
        
        //TODO: Maybe use a prebuilt application loader and edit what's needed into it
        //main.cpp: Change {APPLICATIONLOCATION} with location
        //If icon exists: add 'IDI_ICON1 ICON DISCARDABLE "app.ico"' to app.rc
        /// <summary>
        /// Creates the loader that will be needed for loading the application
        /// </summary>
        /// <param name="tmpFolder">Where the temp folder is</param>
        /// <param name="path">The relative path to the application</param>
        /// <param name="iconLocation">Where the icon is for this application</param>
        /// <param name="outputLocation">Where to put the loader</param>
        /// <param name="applicationName">The application name</param>
        /// <returns>If the loader was created</returns>
        public static bool CreateLoader(string tmpFolder, string path, string? iconLocation, string outputLocation, string applicationName)
        {
            //Only needed for now, hopefully we will make it work on other OS's later on
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Logger.Warning("Not running on Windows, can't create Loader");
                return true;
            }
            
            //TODO: Get based on OS
            //Get stream of template (contained in zip)
            var zipStream = Assembly.GetManifestResourceStream("TinyUpdate.Binary.LoaderTemplate.windows.zip");
            if (zipStream == null)
            {
                Logger.Error("Wasn't able to get zip stream, can't create loader");
                return false;
            }
            
            var templateFolder = Path.Combine(tmpFolder, applicationName, "Loader Template");
            if (Directory.Exists(templateFolder))
            {
                Directory.Delete(templateFolder, true);
            }
            
            //Extract zip
            var templateZip = new ZipArchive(zipStream, ZipArchiveMode.Read);
            templateZip.ExtractToDirectory(templateFolder);
            templateZip.Dispose();
            
            //Drop icon into folder if it exists
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
                Logger.Error("Didn't find APPLICATIONLOCATION in main.cpp, can't create loader");
                return false;
            }
            File.WriteAllLines(mainFileLocation, mainFile);
            
            //Build
            var toolsFile = GetVSTools();
            var cmakeLocation = GetCmake();
            if (string.IsNullOrWhiteSpace(toolsFile))
            {
                Logger.Warning("Unable to find VS Tools. Can't create loader");
                return false;
            }
            toolsFile += @"\VC\Auxiliary\Build\vcvars64.bat";
            if (!File.Exists(toolsFile))
            {
                Logger.Error("Can't see vcvars64.bat file. Do you have the C++ Toolset installed?");
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(cmakeLocation))
            {
                Logger.Warning("Unable to find cmake. Can't create loader");
                return false;
            }
            var buildFolder = Path.Combine(templateFolder, "cmake-build");

            //TODO: Make this OS dependent
            var cacheBat = Path.Combine(templateFolder, "processCache.bat");
            File.WriteAllLines(cacheBat, new []
            {
                $"@call \"{toolsFile}\"",
                $"\"{cmakeLocation}\" -DCMAKE_BUILD_TYPE=Release -G \"CodeBlocks - NMake Makefiles\" -S \"{templateFolder}\" -B \"{buildFolder}\"",
                $"\"{cmakeLocation}\" --build \"{buildFolder}\" --target all"
            });

            var buildProcess = new Process
            {
                StartInfo = new ProcessStartInfo("cmd.exe")
                {
                    Arguments = $"/c \"{cacheBat}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            //Setup grabbing output for us if needed
            var buildLog = File.CreateText(Path.Combine(templateFolder, DateTime.Now.ToFileName() + ".log"));
            buildProcess.OutputDataReceived += (sender, args) => buildLog.WriteLineAsync(args.Data);
            buildProcess.ErrorDataReceived += (sender, args) => buildLog.WriteLineAsync(args.Data);

            //Build
            buildProcess.Start();
            buildProcess.BeginOutputReadLine();
            buildProcess.BeginErrorReadLine();

            //Wait
            buildProcess.WaitForExit();
            buildLog.Dispose();
            if (buildProcess.ExitCode != 0)
            {
                Logger.Error("Failed to create loader");
                return false;
            }
            
            var outputFile = Path.Combine(outputLocation, applicationName + ".exe");
            if (File.Exists(outputFile))
            {
                Logger.Warning("Loader file already exists in output. Deleting old loader");
                File.Delete(outputFile);
            }

            File.Move(Path.Combine(buildFolder, "ApplicationLoader.exe"), outputFile);
            return true;
        }

        private static string? GetCmake()
        {
            var pathContents = (string)Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User)["Path"];
            if (!pathContents.EndsWith(";"))
            {
                pathContents += ';';
            }
            pathContents += (string) Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine)["Path"];

            var variables = pathContents.Split(';');
            foreach (var variable in variables)
            {
                var path = Path.Combine(variable, "cmake.exe");
                if (File.Exists(path))
                {
                    return path;
                }
            }
            return null;
        }
        
        private static string? GetVSTools()
        {
            ManagementObjectCollection mcCollection;

            try
            {
                using ManagementClass mc = new ManagementClass("MSFT_VSInstance");
                mcCollection = mc.GetInstances();
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }

            //We want to buildtools if they have it install, we'll use VS installs if needed
            ManagementBaseObject? vsInstall = null;
            foreach (var result in mcCollection)
            {
                vsInstall ??= result;
                if ((string)result["ProductId"] == "Microsoft.VisualStudio.Product.BuildTools")
                {
                    Logger.Debug("Found VS Build Tools, using that install");
                    return (string)result["InstallLocation"];
                }
            }

            var installLocation = (string?)vsInstall?["InstallLocation"];
            if (string.IsNullOrWhiteSpace(installLocation))
            {
                Logger.Error("Unable to find any VS install or VS Build Tools");
                return null;
            }

            Logger.Information("Found VS install ({0}), using that", vsInstall["Name"]);
            return installLocation;
        }
    }
}