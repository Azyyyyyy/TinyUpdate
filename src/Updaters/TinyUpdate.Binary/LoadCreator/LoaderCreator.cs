using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Temporary;

namespace TinyUpdate.Binary.LoadCreator
{
    public static class LoaderCreator
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(LoaderCreator));
        internal static readonly Assembly Assembly = typeof(LoaderCreatorSource).GetTypeInfo().Assembly;

        //When building from source
        //---------------------------
        //main.cpp: Change {APPLICATIONLOCATION} with location
        //If icon exists: add 'IDI_ICON1 ICON DISCARDABLE "app.ico"' to app.rc

        //When building from pre-built binary
        //-------------------------------------
        //Change {APPLICATIONLOCATION} in binary to path that the application will be
        //Inject icon into binary
        
        /// <summary>
        /// Creates the loader that will be needed for loading the application
        /// </summary>
        /// <param name="tmpFolder">Where the temp folder is</param>
        /// <param name="path">The relative path to the application</param>
        /// <param name="iconLocation">Where the icon is for this application</param>
        /// <param name="outputFile">Where to put the loader</param>
        /// <param name="applicationName">The application name</param>
        /// <param name="intendedOs">What OS this loader is intended for</param>
        /// <returns>If the loader was created</returns>
        public static LoadCreateStatus CreateLoader(TemporaryFolder tmpFolder, string path, string? iconLocation, string outputFile, string applicationName, OSPlatform? intendedOs)
        {
            if (File.Exists(outputFile))
            {
                Logger.Warning("Loader file already exists in output. Deleting old loader");
                File.Delete(outputFile);
            }
            path = path.Replace(@"\", @"\\");

            var prebuiltStatus = LoaderCreatorPrebuilt.CreateLoader(path, iconLocation, outputFile, intendedOs);
            if (prebuiltStatus == LoadCreateStatus.Successful)
            {
                return LoadCreateStatus.Successful;
            }

            Logger.Warning("Unable to use prebuilt loader! (Status: {0})", Enum.GetName(typeof(LoadCreateStatus), prebuiltStatus));
            return LoaderCreatorSource.CreateLoader(tmpFolder, path, iconLocation, outputFile, applicationName);
        }
    }
}