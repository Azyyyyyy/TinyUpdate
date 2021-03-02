using System;
using System.IO;
using TinyUpdate.Core.Logger;

namespace TinyUpdate.Core.Utils
{
    public static class StreamUtil
    {
        private static readonly ILogging Logger = Logging.CreateLogger("StreamUtil");

        public static FileStream? GetFileStreamRead(string fileLocation)
        {
            if (!File.Exists(fileLocation))
            {
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

            return null;
        }
    }
}