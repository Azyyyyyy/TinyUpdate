﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Management;
using System.Runtime.InteropServices;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Helper;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Temporary;

namespace TinyUpdate.Binary.LoadCreator
{
    public static class LoaderCreatorSource
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(LoaderCreatorSource));
        
        /// <summary>
        /// Creates the loader that will be needed for loading the application
        /// </summary>
        /// <param name="tmpFolder">Where the temp folder is</param>
        /// <param name="path">The relative path to the application</param>
        /// <param name="iconLocation">Where the icon is for this application</param>
        /// <param name="outputFile">Where to put the loader</param>
        /// <param name="applicationName">The application name</param>
        /// <returns>If the loader was created</returns>
        public static LoadCreateStatus CreateLoader(TemporaryFolder tmpFolder, string path, string? iconLocation, string outputFile, string applicationName)
        {
            if (OSHelper.ActiveOS != OSPlatform.Windows)
            {
                Logger.Warning("Not running on Windows, can't create loader from source");
                return LoadCreateStatus.UnableToCreate;
            }
            
            //TODO: Get based on OS
            //Get stream of template (contained in zip)
            var zipStream = LoaderCreator.Assembly.GetManifestResourceStream("TinyUpdate.Binary.LoaderTemplate.Windows.source.zip");
            if (zipStream == null)
            {
                Logger.Error("Wasn't able to get zip stream, can't create loader");
                return LoadCreateStatus.Failed;
            }
            
            //Extract zip
            using var templateFolder = tmpFolder.CreateTemporaryFolder(Path.Combine(applicationName, "Loader Template"));
            var templateZip = new ZipArchive(zipStream, ZipArchiveMode.Read);
            templateZip.ExtractToDirectory(templateFolder.Location);
            templateZip.Dispose();
            
            //Drop icon into folder if it exists
            if (!string.IsNullOrWhiteSpace(iconLocation))
            {
                File.Copy(iconLocation, Path.Combine(templateFolder.Location, "app.ico"));
                File.WriteAllText(Path.Combine(templateFolder.Location, "app.rc"), "IDI_ICON1 ICON DISCARDABLE \"app.ico\"");
            }
            
            //Change main.cpp
            var mainFileLocation = Path.Combine(templateFolder.Location, "main.cpp");
            var mainFile = File.ReadAllLines(mainFileLocation);
            var changedContent = false;
            for (int i = 0; i < mainFile.Length; i++)
            {
                if (mainFile[i].Contains("{APPLICATIONLOCATION}"))
                {
                    mainFile[i] = mainFile[i].Replace("{APPLICATIONLOCATION}", path);
                    changedContent = true;
                    break;
                }
            }

            if (!changedContent)
            {
                Logger.Error("Didn't find APPLICATIONLOCATION in main.cpp, can't create loader");
                return LoadCreateStatus.Failed;
            }
            File.WriteAllLines(mainFileLocation, mainFile);
            
            //Build
            var toolsFile = GetVsTools();
            var cmakeLocation = GetCmake();
            if (string.IsNullOrWhiteSpace(toolsFile))
            {
                Logger.Warning("Unable to find VS Tools. Can't create loader");
                return LoadCreateStatus.Failed;
            }
            toolsFile += @"\VC\Auxiliary\Build\vcvars64.bat";
            if (!File.Exists(toolsFile))
            {
                Logger.Error("Can't see vcvars64.bat file. Do you have the C++ Toolset installed?");
                return LoadCreateStatus.Failed;
            }
            if (string.IsNullOrWhiteSpace(cmakeLocation))
            {
                Logger.Warning("Unable to find cmake. Can't create loader");
                return LoadCreateStatus.Failed;
            }
            using var buildFolder = templateFolder.CreateTemporaryFolder("cmake-build");

            //TODO: Make this OS dependent
            using var cacheBat = templateFolder.CreateTemporaryFile("processCache.bat");
            File.WriteAllLines(cacheBat.Location, new []
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
            using var buildLog = templateFolder.CreateTemporaryFile(DateTime.Now.ToFileName() + ".log");
            var buildLogStream = buildLog.GetTextStream();
            buildProcess.OutputDataReceived += (sender, args) => buildLogStream.WriteLineAsync(args.Data);
            buildProcess.ErrorDataReceived += (sender, args) => buildLogStream.WriteLineAsync(args.Data);

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
                Logger.Error(File.ReadAllText(buildLog.Location));
                return LoadCreateStatus.Failed;
            }

            File.Move(Path.Combine(buildFolder.Location, "ApplicationLoader.exe"), outputFile);
            return LoadCreateStatus.Successful;
        }

        private static string? GetCmake()
        {
            var pathContents = (string?)Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User)["Path"] ?? ";";
            if (!pathContents.EndsWith(";"))
            {
                pathContents += ';';
            }
            pathContents += (string?)Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine)["Path"];

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
        
        private static string? GetVsTools()
        {
            //We already check in CreateLoader but makes the compiler happy
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }
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
                if ((string?)result?["ProductId"] == "Microsoft.VisualStudio.Product.BuildTools")
                {
                    Logger.Debug("Found VS Build Tools, using that install");
                    return (string?)result["InstallLocation"];
                }
            }

            var installLocation = (string?)vsInstall?["InstallLocation"];
            if (string.IsNullOrWhiteSpace(installLocation))
            {
                Logger.Error("Unable to find any VS install or VS Build Tools");
                return null;
            }

            Logger.Information("Found VS install ({0}), using that", vsInstall?["Name"]);
            return installLocation;
        }
    }
}