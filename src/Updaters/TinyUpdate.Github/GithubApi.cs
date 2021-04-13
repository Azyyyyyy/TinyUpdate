using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Github
{
    public abstract class GithubApi
    {
        protected readonly HttpClient HttpClient;
        protected readonly ILogging Logger;
        
        protected GithubApi(string apiEndpoint)
        {
            Logger = LoggingCreator.CreateLogger(GetType().Name);
            HttpClient = new HttpClient
            {
                BaseAddress = new Uri(apiEndpoint)
            };
            HttpClient.DefaultRequestHeaders.Add("User-Agent", $"TinyUpdate-{Global.ApplicationName}-{Global.ApplicationVersion}");
        }

        public abstract Task<UpdateInfo?> CheckForUpdate(string organization, string repository);

        public abstract Task<ReleaseNote?> GetChangelog(ReleaseEntry entry, string organization, string repository);
        
        protected async Task<UpdateInfo?> DownloadAndParseReleaseFile(string tagName, long fileSize, string downloadUrl)
        {
            //Download the RELEASE file if we don't already have it
            var releaseFileLoc = Path.Combine(Global.TempFolder, $"RELEASES-{Global.ApplicationName}-{tagName}");
            if (!File.Exists(releaseFileLoc))
            {
                Directory.CreateDirectory(Global.TempFolder);
                using var releaseStream = await HttpClient.GetStreamAsync(downloadUrl);
                using var releaseFileStream = File.Open(releaseFileLoc, FileMode.CreateNew, FileAccess.ReadWrite);
                await releaseStream.CopyToAsync(releaseFileStream);

                //Just do a sanity check
                if (releaseFileStream.Length != fileSize)
                {
                    Logger.Error("RELEASE file isn't the length as expected, deleting and returning null...");
                    releaseFileStream.Dispose();
                    File.Delete(releaseFileLoc);
                    return null;
                }
            }

            //Create the UpdateInfo
            return new UpdateInfo(ReleaseFile.ReadReleaseFile(File.ReadLines(releaseFileLoc)).ToReleaseEntries(tagName));
        }
    }
}