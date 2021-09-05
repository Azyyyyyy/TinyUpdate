using System;
using System.IO;
using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Helper;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Local
{
    public class LocalUpdateClient : UpdateClient
    {
        private readonly string _folderLocation;
        private readonly string _releaseFile;
        private readonly string _updateFileFolder;
        private readonly NoteType _changelogKind;
        public LocalUpdateClient(string folderLocation, IUpdateApplier updateApplier, NoteType changelogKind = NoteType.Markdown) : base(updateApplier)
        {
            _folderLocation = folderLocation;
            _releaseFile = Path.Combine(folderLocation, "RELEASE");
            _updateFileFolder = Path.Combine(AppMetadata.ApplicationFolder, "packages");
            _changelogKind = changelogKind;
            if (!Directory.Exists(folderLocation))
            {
                Logger.Warning("{0} directory doesn't exist, this will cause this UpdateClient to not function", folderLocation);
            }
        }

        public override Task<UpdateInfo?> CheckForUpdate(bool grabDeltaUpdates = true)
        {
            return Task.FromResult(ReleaseFileExt.GetUpdateInfo(_releaseFile, AppMetadata, 
                grabDeltaUpdates, folderLocation: _updateFileFolder));
        }

        public override Task<ReleaseNote?> GetChangelog(ReleaseEntry entry)
        {
            var changelogFile = Path.Combine(_folderLocation, "changelogs", "changelog-" + entry.Version);
            if (!File.Exists(changelogFile))
            {
                return Task.FromResult<ReleaseNote?>(null);
            }

            return Task.FromResult<ReleaseNote?>(new ReleaseNote(File.ReadAllText(changelogFile), _changelogKind));
        }

        public override async Task<bool> DownloadUpdate(ReleaseEntry releaseEntry, Action<double>? progress)
        {
            //No need to copy the file if it's what we expect already
            if (releaseEntry.IsValidReleaseEntry(AppMetadata.ApplicationVersion, true))
            {
                Logger.Information("{0} already exists and is what we expect, working with that", releaseEntry.FileLocation);
                return true;
            }

            var bytesWritten = 0d;
            using var releaseStream = FileHelper.OpenWrite(releaseEntry.FileLocation, releaseEntry.Filesize);
            using var packageStream = new ProgressStream(
                FileHelper.MakeFileStream(Path.Combine(_folderLocation, releaseEntry.Filename), FileMode.Open, FileAccess.ReadWrite),
                (count =>
                {
                    bytesWritten += count;
                    progress?.Invoke(bytesWritten / releaseEntry.Filesize);
                }));

            await packageStream.CopyToAsync(releaseStream);
            return releaseEntry.CheckReleaseEntry(AppMetadata.ApplicationVersion, true);
        }
    }
}