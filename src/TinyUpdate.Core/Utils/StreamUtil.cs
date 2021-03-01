using System;
using System.Diagnostics;
using System.IO;

namespace TinyUpdate.Core.Utils
{
    public static class StreamUtil
    {
        
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
                Trace.WriteLine(e);
            }

            return null;
        }
    }
}