using System;
using System.Threading.Tasks;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Core.Extensions
{
    public static class UpdateClientExt
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(UpdateClientExt));
        
        public static async Task<UpdateStatus> UpdateApp(this UpdateClient updateClient, Action<double>? progress)
        {
            //Check
            var updateInfo = await updateClient.CheckForUpdate();
            if (updateInfo is not { HasUpdate: true })
            {
                return UpdateStatus.NoUpdate;
            }
            Logger.Information("We got an update from {0} to {1}", updateClient.AppMetadata.ApplicationVersion, updateInfo.NewVersion);

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
}