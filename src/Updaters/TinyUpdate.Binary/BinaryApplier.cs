using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using TinyUpdate.Binary.Delta;
using TinyUpdate.Core;
using TinyUpdate.Core.Helper;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Update;
using TinyUpdate.Core.Utils;
using TinyUpdate.Binary.Entry;
using TinyUpdate.Binary.Extensions;

namespace TinyUpdate.Binary
{
    /// <summary>
    /// Applies updates that the <see cref="BinaryCreator"/> has created
    /// </summary>
    public class BinaryApplier : IUpdateApplier
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(BinaryApplier));

        /// <inheritdoc cref="IUpdateApplier.GetApplicationPath(Version?)"/>
        public string? GetApplicationPath(Version? version) =>
            version == null ? null : Path.Combine(Global.ApplicationFolder, $"app-{version}");

        /// <inheritdoc cref="IUpdateApplier.Extension"/>
        public string Extension { get; } = ".tuup";

        /// <inheritdoc cref="IUpdateApplier.ApplyUpdate(ReleaseEntry, Action{decimal})"/>
        public async Task<bool> ApplyUpdate(ReleaseEntry entry, Action<decimal>? progress = null)
        {
            //Check that we was the one who made the update (as shown with the file extension)
            if (Path.GetExtension(entry.Filename) != Extension)
            {
                Logger.Error("{0} is not a update made by {1}, bail...", entry.FileLocation, nameof(BinaryApplier));
                return false;
            }

            //Get the paths for where the version will be and where the older version is
            var newPath = GetApplicationPath(entry.Version);
            var basePath = GetApplicationPath(Global.ApplicationVersion);

            /*Make sure the basePath exists, can't do a delta update without it!
             (This also shows that something is *REALLY* wrong)*/
            if (!Directory.Exists(basePath))
            {
                Logger.Error("{0} doesn't exist, can't update", basePath);
                return false;
            }

            /*Delete the folder if it exist, likely that application
             was closed while we was updating*/
            newPath.CheckForFolder();

            //Do the update!
            return await ApplyUpdate(basePath, newPath, entry, progress);
        }

        /// <inheritdoc cref="IUpdateApplier.ApplyUpdate(UpdateInfo, Action{decimal})"/>
        public async Task<bool> ApplyUpdate(UpdateInfo updateInfo, Action<decimal>? progress = null)
        {
            //Check that we have some kind of update to apply
            if (!updateInfo.HasUpdate)
            {
                Logger.Error("We don't have a update to apply!!");
                return false;
            }

            //Check that we aren't trying to process both delta and full updates
            if (updateInfo.Updates.Any(x => x.IsDelta)
                && updateInfo.Updates.Any(x => !x.IsDelta))
            {
                Logger.Error("You can't mix delta and full updates to be installed!!");
                return false;
            }
            
            //Check that if we are doing a full package then they isn't more then one
            if (updateInfo.Updates.Count(x => !x.IsDelta) > 1)
            {
                Logger.Error("We shouldn't be given more then one full update!!");
                return false;
            }

            //Check that we can bounce from each update that needs to be applied
            var updates = updateInfo.Updates.OrderBy(x => x.Version).ToArray();
            var lastUpdateVersion = Global.ApplicationVersion;
            foreach (var update in updates)
            {
                //We don't need to do this if we are using a full package
                if (update.IsDelta
                    && (update.OldVersion?.Equals(lastUpdateVersion) ?? false))
                {
                    Logger.Error("Can't update to {0} due to update not being created from {1}", update.Version,
                        lastUpdateVersion);
                    return false;
                }

                lastUpdateVersion = update.Version;
            }

            //Get the **newest** version that we have to apply and use that for the folder
            var newVersionFolder = GetApplicationPath(updateInfo.NewVersion);
            if (string.IsNullOrWhiteSpace(newVersionFolder))
            {
                Logger.Error("Can't get folder for the new version");
                return false;
            }

            /*Delete the folder if it exist, likely that application
             was closed while we was updating*/
            newVersionFolder.CheckForFolder();

            /*Go through every update we have, reporting the
             progress by how many updates we have*/
            var updateCounter = 0m;
            var updateCount = updateInfo.Updates.Length;
            var doneFirstUpdate = false;
            foreach (var updateEntry in updates)
            {
                /*Base path changes based on if the first update has been
                 done as we want to start of using the old files we have*/
                if (!await ApplyUpdate(
                    doneFirstUpdate ? newVersionFolder : GetApplicationPath(Global.ApplicationVersion),
                    newVersionFolder,
                    updateEntry,
                    updateProgress => progress?.Invoke((updateProgress + updateCounter) / updateCount)))
                {
                    Logger.Error("Applying version {0} failed", updateEntry.Version);
                    return false;
                }

                updateCounter++;
                doneFirstUpdate = true;
            }

            return true;
        }

        /// <inheritdoc cref="ApplyUpdate(ReleaseEntry, Action{decimal})"/>
        /// <param name="basePath">Path where we grab any old files that can be reused from</param>
        /// <param name="newPath">Where to put the new version of the application into</param>
        /// <param name="entry"></param>
        /// <param name="progress"></param>
        private static async Task<bool> ApplyUpdate(string basePath, string newPath, ReleaseEntry entry,
            Action<decimal>? progress = null)
        {
            if (!File.Exists(entry.FileLocation))
            {
                Logger.Error("Update file doesn't exist...");
                return false;
            }

            //If we fail then delete the file, it's better to re-download then to get a virus!
            if (!entry.IsValidReleaseEntry(true))
            {
                Logger.Error("Update file doesn't match what we expect... deleting update file and bailing");
                File.Delete(entry.FileLocation);
                return false;
            }

            //Grab all the files from the update file
            var zip = new ZipArchive(File.OpenRead(entry.FileLocation));
            var updateEntry = await zip.CreateUpdateEntry();
            if (updateEntry == null || updateEntry.Count == 0)
            {
                /*This only happens when something is up with the update file,
                  delete and return false*/
                Logger.Error(
                    "Something happened while grabbing files in update file... deleting update file and bailing");
                zip.Dispose();
                File.Delete(entry.FileLocation);
                return false;
            }

            void Cleanup()
            {
                zip.Dispose();
                progress?.Invoke(1);
            }

            //We need that extra 1 so we are at 99% when done (we got some cleanup to do after)
            var fileCounter = 0m;
            var filesCount = updateEntry.Count + 1;

            void UpdateProgress(decimal? fileCount = null) => progress?.Invoke((fileCount ?? fileCounter) / filesCount);

            //We want to do the files that didn't change first
            foreach (var file in updateEntry.SameFile)
            {
                fileCounter++;
                Logger.Debug("Processing {0}", file.FileLocation);

                //Create folder (if it exists)
                newPath.CreateDirectory(file.FolderPath);

                //Get where the old and "new" file should be
                var originalFile = Path.Combine(basePath, file.FileLocation);
                var newFile = Path.Combine(newPath, file.FileLocation);

                /*Check that the files exists and that we was 
                  able to process file, if not then hard bail!*/
                if (!ProcessSameFile(originalFile, newFile))
                {
                    file.Stream?.Dispose();
                    Cleanup();
                    return false;
                }

                UpdateProgress();
                file.Stream?.Dispose();
            }

            //Note that this should be the only loop that is used when doing a full update
            foreach (var newFile in updateEntry.NewFile)
            {
                fileCounter++;
                Logger.Debug("Processing {0}", newFile.FileLocation);

                //Create folder (if it exists)
                newPath.CreateDirectory(newFile.FolderPath);

                var newFileLocation = Path.Combine(newPath, newFile.FileLocation);
                var applySuccessful = await ProcessNewFile(newFile, newFileLocation);

                //Check that it applied and is what we are expecting
                if (!CheckUpdatedFile(applySuccessful, newFileLocation, newFile))
                {
                    newFile.Stream?.Dispose();
                    Cleanup();
                    return false;
                }

                UpdateProgress();
                newFile.Stream?.Dispose();
            }

            foreach (var deltaFile in updateEntry.DeltaFile)
            {
                Logger.Debug("Processing {0}", deltaFile.FileLocation);

                //Create folder (if it exists)
                newPath.CreateDirectory(deltaFile.FolderPath);

                var originalFile = Path.Combine(basePath, deltaFile.FileLocation);
                var newFileLocation = Path.Combine(newPath, deltaFile.FileLocation);

                var applySuccessful = await DeltaApplying.ProcessDeltaFile(originalFile, newFileLocation, deltaFile,
                    applyProgress => UpdateProgress(fileCounter + applyProgress));

                /*We up this counter after as a delta update can report the progress of it being applied,
                 upping it before will make the first delta update jump up the progress more then it should*/
                fileCounter++;

                //Check that it applied and is what we are expecting
                if (!CheckUpdatedFile(applySuccessful, newFileLocation, deltaFile))
                {
                    deltaFile.Stream?.Dispose();
                    Cleanup();
                    return false;
                }

                UpdateProgress();
                deltaFile.Stream?.Dispose();
            }

            //Check for any dead files and delete them
            var filesLocation = updateEntry.All.Select(x => Path.Combine(newPath, x.FileLocation)).ToArray();
            foreach (var file in Directory.EnumerateFiles(newPath))
            {
                if (!filesLocation.Contains(file))
                {
                    Logger.Debug("{0} no-longer exists in version {1}, deleting....", file, entry.Version);
                    File.Delete(file);
                }
            }

            Cleanup();
            return true;
        }

        /// <summary>
        /// Checks that the file applied is the file that should of been applied
        /// </summary>
        /// <param name="applySuccessful">If applying the update was successful</param>
        /// <param name="fileLocation">Where the file is</param>
        /// <param name="fileEntry">Details about the file</param>
        /// <returns>If it's what we expect</returns>
        private static bool CheckUpdatedFile(bool applySuccessful, string fileLocation, FileEntry fileEntry)
        {
            if (!applySuccessful)
            {
                Logger.Error("Applying {0} wasn't successful", fileEntry.FileLocation);
                return false;
            }

            //This file didn't change, assume it's fine
            if (fileEntry.Filesize == 0)
            {
                Logger.Debug("Filesize is 0, we should be fine");
                return true;
            }

            //Check file size and hash
            var fileStream = File.OpenRead(fileLocation);
            var isExpectedFile =
                fileStream.Length == fileEntry.Filesize &&
                SHA256Util.CreateSHA256Hash(fileStream) == fileEntry.SHA256;
            fileStream.Dispose();

            /*Delete file if it's not expected, it's better for them to 
             redownload/reinstall then to catch a virus!*/
            if (!isExpectedFile)
            {
                Logger.Error("Updated file is not what we expect, deleting it!");
                File.Delete(fileLocation);
            }

            return isExpectedFile;
        }

        /// <summary>
        /// Processes a file that is the same in both versions
        /// </summary>
        /// <param name="originalFile">Where the original file is located</param>
        /// <param name="newFile">Where the "new" file will be</param>
        /// <returns>If we was able to process the file</returns>
        private static bool ProcessSameFile(string originalFile, string newFile)
        {
            /*If we got here then it means that we are working on a file that
             we *should* have, check that is the case*/
            Logger.Debug("File wasn't updated, making sure file exists and then making hard link");
            if (!File.Exists(originalFile))
            {
                Logger.Error("File that we need to copy doesn't exist!");
                return false;
            }

            //We already put the file in place from another update
            if (newFile == originalFile)
            {
                Logger.Debug("Files given are the same, we should be fine");
                return true;
            }

            //The file shouldn't yet exist, delete it just to be safe
            if (File.Exists(newFile))
            {
                Logger.Warning("There was a file at {0} when there shouldn't been at this stage", newFile);
                File.Delete(newFile);
            }

            //Try to create the hard link
            if (HardLinkHelper.CreateHardLink(originalFile, newFile))
            {
                return true;
            }

            //We wasn't able to hard link, just try to copy the file
            Logger.Warning("Wasn't able to create hard link, just going to copy the file");
            try
            {
                File.Copy(originalFile, newFile);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Couldn't copy {0} to {1}", originalFile, newFile);
                Logger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Copies the file that was in the update file
        /// </summary>
        /// <param name="fileEntry">Entry with the file that needs to be copied</param>
        /// <param name="fileLocation">Where the file should go</param>
        /// <returns>If we was able to process the file</returns>
        private static async Task<bool> ProcessNewFile(FileEntry fileEntry, string fileLocation)
        {
            if (fileEntry.Stream == null)
            {
                Logger.Error("fileEntry's doesn't have a stream, can't copy file that would be at {0}", fileLocation);
                return false;
            }

            var fileStream = File.OpenWrite(fileLocation);
            await fileEntry.Stream.CopyToAsync(fileStream);
            fileStream.Dispose();
            return true;
        }
    }
}