using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Update;
using TinyUpdate.Http.Extensions;

namespace TinyUpdate.Http
{
    public class HttpUpdateClient : UpdateClient
    {
        protected readonly ProgressMessageHandler _progressMessageHandler = new();
        protected readonly HttpClient _httpClient;
        protected readonly NoteType NoteType;

        public HttpUpdateClient(string uri, IUpdateApplier updateApplier, NoteType changelogKind = NoteType.Markdown) 
            : this(new Uri(uri), updateApplier, changelogKind) { }
        
        public HttpUpdateClient(Uri uri, IUpdateApplier updateApplier, NoteType changelogKind = NoteType.Markdown) 
            : base(updateApplier)
        {
            _httpClient = HttpClientFactory.Create(new HttpClientHandler(), _progressMessageHandler);
            _httpClient.BaseAddress = uri;
            NoteType = changelogKind;
        }

        public override async Task<UpdateInfo?> CheckForUpdate(bool grabDeltaUpdates = true)
        {
            var releaseFileLocation = Path.Combine(ApplicationMetadata.TempFolder, "RELEASE");
            //if this is the case then we clearly haven't downloaded the RELEASE file
            if (await DownloadReleaseFile(releaseFileLocation, _httpClient.BaseAddress + "RELEASE") < 0)
            {
                return null;
            }

            //Create the UpdateInfo
            return ReleaseFileExt.GetUpdateInfo(releaseFileLocation, ApplicationMetadata, grabDeltaUpdates);
        }

        public override async Task<ReleaseNote?> GetChangelog(ReleaseEntry entry)
        {
            var response =
                await _httpClient.GetResponseMessage(new HttpRequestMessage(HttpMethod.Get,
                    _httpClient.BaseAddress + "changelogs/" + "changelog-" + entry.Version));

            if (response is not { IsSuccessStatusCode: true })
            {
                return null;
            }

            return new ReleaseNote(await response.Content.ReadAsStringAsync(), NoteType);
        }

        public override async Task<bool> DownloadUpdate(ReleaseEntry releaseEntry, Action<double>? progress)
        {
            //If this is the case then we already have the file and it's what we expect, no point in downloading it again
            if (releaseEntry.IsValidReleaseEntry(ApplicationMetadata.ApplicationVersion, true))
            {
                return true;
            }
            
            void ReportProgress(object? sender, HttpProgressEventArgs args)
            {
                progress?.Invoke((double) args.BytesTransferred / releaseEntry.Filesize);
            }

            //Download the file
            Logger.Information("Downloading file {0} ({1})", releaseEntry.Filename, releaseEntry.FileLocation);
            _progressMessageHandler.HttpReceiveProgress += ReportProgress;
            var successfullyDownloaded = await DownloadUpdateInter(releaseEntry);
            _progressMessageHandler.HttpReceiveProgress -= ReportProgress;

            //Check the file
            Logger.Debug("Successfully downloaded?: {0}", successfullyDownloaded);
            return releaseEntry.CheckReleaseEntry(ApplicationMetadata.ApplicationVersion, successfullyDownloaded);
        }

        protected virtual string GetUriForReleaseEntry(ReleaseEntry releaseEntry) =>
            _httpClient.BaseAddress + releaseEntry.Filename;
        
        private async Task<bool> DownloadUpdateInter(ReleaseEntry releaseEntry)
        {
            try
            {
                using var releaseStream = await _httpClient.GetStreamAsync(GetUriForReleaseEntry(releaseEntry));

                //Delete the file if it already exists
                if (File.Exists(releaseEntry.FileLocation))
                {
                    Logger.Warning("{0} already exists, going to delete it", releaseEntry.FileLocation);
                    File.Delete(releaseEntry.FileLocation);
                }
            
                using var releaseFileStream = File.Open(releaseEntry.FileLocation, FileMode.CreateNew, FileAccess.ReadWrite);
                await releaseStream.CopyToAsync(releaseFileStream);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        protected async Task<long> DownloadReleaseFile(string releaseFileLocation, string downloadUrl)
        {
            //Download the RELEASE file if we don't already have it
            var releaseFileInfo = new FileInfo(releaseFileLocation);
            long? fileLength = null;

            //See if it was recently downloaded, if not delete it
            if (releaseFileInfo.Exists && DateTime.Now.Subtract(releaseFileInfo.LastWriteTime).TotalDays >= 1)
            {
                releaseFileInfo.Delete();
            }
            
            if (!releaseFileInfo.Exists)
            {
                Directory.CreateDirectory(ApplicationMetadata.TempFolder);
                var response = await _httpClient.GetResponseMessage(new HttpRequestMessage(HttpMethod.Get, downloadUrl));
                if (response == null)
                {
                    Logger.Error("Didn't get anything from Github, can't download");
                    return -1;
                }
                
                using var releaseStream = await response.Content.ReadAsStreamAsync();
                using var releaseFileStream = File.Open(releaseFileInfo.FullName, FileMode.CreateNew, FileAccess.ReadWrite);
                await releaseStream.CopyToAsync(releaseFileStream);
                fileLength = releaseFileStream.Length;
            }
            return fileLength ?? releaseFileInfo.Length;
        }
    }
}