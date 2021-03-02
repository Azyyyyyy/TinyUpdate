using System;
using System.IO;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Core.Utils
{
    /// <summary>
    /// Lets us safely grab streams without 
    /// </summary>
    public static class StreamUtil
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger("StreamUtil");

        /// <summary>
        /// Provides a <see cref="FileStream"/> after doing some checking
        /// </summary>
        /// <param name="fileLocation">File to grab</param>
        public static FileStream? GetFileStreamRead(string fileLocation)
        {
            if (!File.Exists(fileLocation))
            {
                Logger.Warning("{0} doesn't exist, can't open", fileLocation);
                return null;
            }

            try
            {
                return File.OpenRead(fileLocation);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            Logger.Warning("Couldn't open {0}", fileLocation);
            return null;
        }
    }
}