using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Helper;
using TinyUpdate.Core.Update;
using TinyUpdate.Http.Extensions;

namespace TinyUpdate.Http
{
    public class HttpUpdateClient : UpdateClient
    {
        protected HttpClient _httpClient;
        protected readonly NoteType NoteType;

        public HttpUpdateClient(string uri, IUpdateApplier updateApplier, NoteType changelogKind = NoteType.Markdown) 
            : this(new Uri(uri), updateApplier, changelogKind) { }
        
        public HttpUpdateClient(Uri uri, IUpdateApplier updateApplier, NoteType changelogKind = NoteType.Markdown) 
            : base(updateApplier)
        {
            _httpClient = new HttpClient { BaseAddress = uri };
            NoteType = changelogKind;
        }

        public override async Task<UpdateInfo?> CheckForUpdate(bool grabDeltaUpdates = true)
        {
            var releaseFileLocation = Path.Combine(AppMetadata.TempFolder, "RELEASE");
            //if this is the case then we clearly haven't downloaded the RELEASE file
            if (await DownloadReleaseFile(releaseFileLocation) < 0)
            {
                return null;
            }

            //Create the UpdateInfo
            return ReleaseFileExt.GetUpdateInfo(releaseFileLocation, AppMetadata, grabDeltaUpdates);
        }

        public override async Task<ReleaseNote?> GetChangelog(ReleaseEntry entry)
        {
            using var response =
                await _httpClient.GetResponseMessage(new HttpRequestMessage(HttpMethod.Get, GetUriForChangelog(entry)));
            if (response is not { IsSuccessStatusCode: true })
            {
                return null;
            }

            using var streamReader = new StreamReader(await GetStreamFromResponse(response));
            return new ReleaseNote(await streamReader.ReadToEndAsync(), NoteType);
        }

        public override async Task<bool> DownloadUpdate(ReleaseEntry releaseEntry, Action<double>? progress = default)
        {
            //If this is the case then we already have the file and it's what we expect, no point in downloading it again
            if (releaseEntry.IsValidReleaseEntry(AppMetadata.ApplicationVersion, true))
            {
                return true;
            }
            
            double bytesWritten = 0;

            //Download the file
            Logger.Information("Downloading file {0} ({1})", releaseEntry.Filename, releaseEntry.FileLocation);
            var successfullyDownloaded = await DownloadUpdateInter(releaseEntry, i =>
            {
                bytesWritten += i;
                progress?.Invoke(bytesWritten / releaseEntry.Filesize);
            });

            //Check the file
            Logger.Debug("Successfully downloaded?: {0}", successfullyDownloaded);
            return releaseEntry.CheckReleaseEntry(AppMetadata.ApplicationVersion, successfullyDownloaded);
        }

        protected virtual async Task<bool> DownloadUpdateInter(ReleaseEntry releaseEntry, Action<int>? progress)
        {
            try
            {
				var path = Directory.GetParent(releaseEntry.FileLocation);
				if (path == null)
				{
					Logger.Error("Unable to get details needed for putting file on disk");
					return false;
				}
				Directory.CreateDirectory(path.FullName);
				
                using var response = await _httpClient.GetResponseMessage(new HttpRequestMessage(HttpMethod.Get, GetUriForReleaseEntry(releaseEntry)));
                if (response is not { IsSuccessStatusCode: true })
                {
                    return false;
                }
                using var releaseStream = await GetStreamFromResponse(response);
                
                //Delete the file if it already exists
                if (File.Exists(releaseEntry.FileLocation))
                {
                    Logger.Warning("{0} already exists, going to delete it", releaseEntry.FileLocation);
                    File.Delete(releaseEntry.FileLocation);
                }
            
                var releaseFileStream = new ProgressStream(
                    FileHelper.MakeFileStream(releaseEntry.FileLocation, FileMode.CreateNew, FileAccess.ReadWrite, releaseEntry.Filesize), writeAction: (count) => progress?.Invoke(count));
                await releaseStream.CopyToAsync(releaseFileStream);
                releaseFileStream.Dispose();

                if (!releaseEntry.IsValidReleaseEntry(AppMetadata.ApplicationVersion, true))
				{
					File.Delete(releaseEntry.FileLocation);
					return false;
				}
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        protected async Task<long> DownloadReleaseFile(string releaseFileLocation)
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
                Directory.CreateDirectory(AppMetadata.TempFolder);
                using var response = await _httpClient.GetResponseMessage(new HttpRequestMessage(HttpMethod.Get, GetUriForReleaseFile()));
                if (response is not { IsSuccessStatusCode: true })
                {
                    Logger.Error("Didn't get anything, can't download");
                    return -1;
                }
                
                using var releaseStream = await GetStreamFromResponse(response);
                using var releaseFileStream = FileHelper.MakeFileStream(releaseFileInfo.FullName, FileMode.CreateNew, FileAccess.ReadWrite, releaseStream.Length);
                await releaseStream.CopyToAsync(releaseFileStream);
                fileLength = releaseStream.Length;
            }
            return fileLength ?? releaseFileInfo.Length;
        }

        protected virtual string GetUriForReleaseEntry(ReleaseEntry releaseEntry) =>
            _httpClient.BaseAddress + releaseEntry.Filename;
        
        protected virtual string GetUriForReleaseFile() =>
            _httpClient.BaseAddress + "RELEASE";

        protected virtual string GetUriForChangelog(ReleaseEntry entry) => _httpClient.BaseAddress + "changelogs/" + "changelog-" + entry.Version;
        
        protected virtual Task<Stream> GetStreamFromResponse(HttpResponseMessage message) => message.Content.ReadAsStreamAsync();
    }
}