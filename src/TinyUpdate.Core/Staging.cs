using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Core;

public static class Staging
{
    private static readonly ILogger Logger = LogManager.CreateLogger(nameof(Staging));

    //Adapted from Squirrel.Windows: https://github.com/Squirrel/Squirrel.Windows/blob/develop/src/Squirrel/ReleaseEntry.cs
    public static bool IsStagingMatch(this ReleaseEntry releaseEntry, byte[]? userId)
    {
        // A "Staging match" is when a user falls into the affirmative
        // bucket - i.e. if the staging is at 10%, this user is the one out
        // of ten case.
        if (!releaseEntry.StagingPercentage.HasValue)
        {
            return true;
        }
        if (userId is not { Length: 4 })
        {
            return false;
        }

        double val = BitConverter.ToUInt32(userId, 0);
        var percentage = val / uint.MaxValue;
        return percentage * 100 > releaseEntry.StagingPercentage.Value;
    }

    public static string UserIdAsString(byte[] by) => 
        string.Join("-", by.Select(x => x.ToString()));

    private static readonly Random Prng = new Random();
    private static readonly Dictionary<string, byte[]> KnownUserIds = new Dictionary<string, byte[]>();
        
    //Adapted from Squirrel.Windows: https://github.com/Squirrel/Squirrel.Windows/blob/develop/src/Squirrel/UpdateManager.CheckForUpdates.cs
    public static byte[]? GetOrCreateStagedUserId(string appDirectory)
    {
        //If we done this already then no need to grab it off disk again
        if (KnownUserIds.TryGetValue(appDirectory, out var bytes))
        {
            Logger.Log(Level.Info, $"Already have the User ID cached for this application ({UserIdAsString(bytes)})");
            return bytes;
        }

        var stagedUserIdFolder = Path.Combine(appDirectory, "packages");
        var stagedUserIdFile = Path.Combine(stagedUserIdFolder, ".betaId");

        var fileExists = File.Exists(stagedUserIdFile);
        bytes = fileExists ? File.ReadAllBytes(stagedUserIdFile) : null;
        if (fileExists && bytes!.Length == 4)
        {
            Logger.Log(Level.Info, $"Using existing staging user ID: {UserIdAsString(bytes)}");
            KnownUserIds.Add(appDirectory, bytes);
            return bytes;
        }
        Logger.Error("File was read but contents were invalid, creating a new user ID");

        var buf = new byte[4096];
        Prng.NextBytes(buf);
            
        var ret = GuidUtil.CreateGuidFromHash(buf, GuidUtil.IsoOidNamespace).ToByteArray();
        buf = new byte[4];
        Array.Copy(ret, 12, buf, 0, 4);

        try
        {
            Directory.CreateDirectory(stagedUserIdFolder);
            File.WriteAllBytes(stagedUserIdFile, buf);
            KnownUserIds.Add(appDirectory, buf);

            Logger.Log(Level.Info, $"Generated new staging user ID ({UserIdAsString(buf)})");
            return buf;
        } 
        catch (Exception ex) 
        {
            Logger.Warn("Unable to write staging user ID to disk");
            Logger.Log(ex);
            return null;
        }
    }
}