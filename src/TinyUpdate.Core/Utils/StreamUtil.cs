using System;
using System.IO;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Core.Utils;

/// <summary>
/// Lets us safely grab streams 
/// </summary>
public static class StreamUtil
{
    private static readonly ILogger Logger = LogManager.CreateLogger(nameof(StreamUtil));

    /// <summary>
    /// Provides a <see cref="FileStream"/> after doing some checking
    /// </summary>
    /// <param name="fileLocation">File to grab</param>
    public static FileStream? SafeOpenRead(string fileLocation)
    {
        if (!File.Exists(fileLocation))
        {
            Logger.Log(Level.Warn, $"{fileLocation} doesn't exist, can't open");
            return null;
        }

        try
        {
            return File.OpenRead(fileLocation);
        }
        catch (Exception e)
        {
            Logger.Log(e);
        }

        Logger.Log(Level.Warn, $"Couldn't open {fileLocation}");
        return null;
    }
}