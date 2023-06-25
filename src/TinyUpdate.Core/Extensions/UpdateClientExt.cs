using System;
using System.Threading.Tasks;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Core.Extensions;

public static class UpdateClientExt
{
    private static readonly ILogger Logger = LogManager.CreateLogger(nameof(UpdateClientExt));
        
    public static async Task<UpdateStatus> UpdateApp(this UpdateClient updateClient, Action<double>? progress)
    {
        //Check
        var updateInfo = await updateClient.CheckForUpdate(true);
        if (updateInfo is not { HasUpdate: true })
        {
            //Check for full updates if we can't get a delta update
            updateInfo = await updateClient.CheckForUpdate(false);
            if (updateInfo is not { HasUpdate: true })
            {
                return UpdateStatus.NoUpdate;
            }
        }
        Logger.Log(Level.Info,$"We got an update from {updateClient.AppMetadata.ApplicationVersion} to {updateInfo.NewVersion}");

        //Download
        var downloadSuccessful = await updateClient.DownloadUpdate(updateInfo, d => progress?.Invoke(d / 2));
        if (!downloadSuccessful)
        {
            Logger.Error("We wasn't able to download the updates");
            return UpdateStatus.DownloadFailed;
        }

        //Install
        var installSuccessful = await updateClient.ApplyUpdate(updateInfo, d => progress?.Invoke((d / 2) + 0.5));
        if (!installSuccessful)
        {
            Logger.Error("We wasn't able to install the updates");
            return UpdateStatus.InstallFailed;
        }
        return UpdateStatus.Success;
    }
}

public enum UpdateStatus
{
    NoUpdate,
    DownloadFailed,
    InstallFailed,
    Success
}