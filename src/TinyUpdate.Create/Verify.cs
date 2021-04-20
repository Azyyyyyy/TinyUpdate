using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Extensions;
using TinyUpdate.Core.Update;
using TinyUpdate.Core.Utils;
using TinyUpdate.Create.AssemblyHelper;
using TinyUpdate.Create.Helper;

namespace TinyUpdate.Create
{
    public static class Verify
    {
        private static readonly CustomConsoleLogger Logger = new(nameof(Verify));

        public static async Task VerifyUpdateFiles(string extension, string? applierType)
        {
            if (Global.SkipVerify)
            {
                return;
            }
            
            //Ask if they want to verify files
            if (Global.AskIfUserWantsToVerify)
            {
                if (!ConsoleHelper.RequestYesOrNo("Do you want us to verify any created updates?", true))
                {
                    return;
                }
                Logger.WriteLine("");
            }

            //Grab the applier that we will be using
            var applier = GetAssembly.GetTypeFromAssembly<IUpdateApplier>("applier", applierType);
            if (applier == null)
            {
                Logger.Error("Can't get applier, can't verify update...");
                return;
            }
            Logger.WriteLine("Setting up for verifying update files");

            //Setup Global with what it would have if in the application
            var applicationLocation = Path.Combine(Core.Global.TempFolder, Global.MainApplicationName);
            Directory.CreateDirectory(applicationLocation);
            Core.Global.ApplicationFolder = applicationLocation;
            Core.Global.ApplicationVersion = Global.ApplicationOldVersion ?? new Version(0, 0, 0, 1);
            var oldVersionLocation = applier.GetApplicationPath(Core.Global.ApplicationVersion);

            //Delete the old version if it exists, likely here from verifying update files last time
            if (Directory.Exists(oldVersionLocation))
            {
                Directory.Delete(oldVersionLocation, true);
            }
            Directory.CreateDirectory(oldVersionLocation);

            //Grab the files that we will need to copy
            var folderToCopy = Global.OldVersionLocation ?? Global.NewVersionLocation;
            foreach (var file in Directory.EnumerateFiles(folderToCopy, "*", SearchOption.AllDirectories))
            {
                var fileLocation = Path.Combine(oldVersionLocation, file.Remove(0, folderToCopy.Length + 1));
                var folder = Path.GetDirectoryName(fileLocation);
                if (string.IsNullOrWhiteSpace(folder))
                {
                    Logger.Error("Can't get folder from the application, can't continue (From file {0})", fileLocation);
                    return;
                }
                Directory.CreateDirectory(folder);
                
                File.Copy(file, fileLocation);
            }

            //Now verify the updates
            var fullUpdateFileLocation = Program.GetOutputLocation(false, extension);
            if (Global.CreateFullUpdate && File.Exists(fullUpdateFileLocation))
            {
                ConsoleHelper.ShowSuccess(
                    await VerifyUpdate(fullUpdateFileLocation, false, Global.ApplicationOldVersion, Global.ApplicationNewVersion, applier));
                Logger.WriteLine("");
            }
            
            var deltaUpdateFileLocation = Program.GetOutputLocation(true, extension);
            if (Global.CreateDeltaUpdate && File.Exists(deltaUpdateFileLocation))
            {
                ConsoleHelper.ShowSuccess(
                    await VerifyUpdate(deltaUpdateFileLocation, true, Global.ApplicationOldVersion, Global.ApplicationNewVersion, applier));
            }
        }

        private static async Task<bool> VerifyUpdate(string updateFile, bool isDelta, Version? oldVersion, Version newVersion, IUpdateApplier updateApplier)
        {
            //Grab the hash and size of this update file
            Logger.WriteLine("Applying update file {0} to test applying and to be able to cross check files", updateFile);
            await using var updateFileStream = File.OpenRead(updateFile);
            var shaHash = SHA256Util.CreateSHA256Hash(updateFileStream);
            var filesize = updateFileStream.Length;
            await updateFileStream.DisposeAsync();

            //Create the release entry
            var entry = new ReleaseEntry(
                shaHash, 
                Path.GetFileName(updateFile), 
                filesize, 
                isDelta, 
                newVersion,
                Path.GetDirectoryName(updateFile), 
                oldVersion);

            //Try to do the update
            var applyProgressBar = new ProgressBar();
            var stopwatch = new Stopwatch();

            stopwatch.Start();
            var successful = await updateApplier.ApplyUpdate(entry, progress => applyProgressBar.Report((double)progress));
            stopwatch.Stop();
            applyProgressBar.Dispose();
            
            Logger.WriteLine("Processing this update took {0}", ConsoleHelper.TimeSpanToString(stopwatch.Elapsed));

            //Error out if we wasn't able to apply update
            if (!successful)
            {
                return false;
            }
            
            //Grab files that we have
            var newVersionFiles = Directory.GetFiles(Global.NewVersionLocation,"*", SearchOption.AllDirectories);
            var appliedVersionFiles = Directory.GetFiles(updateApplier.GetApplicationPath(newVersion),"*", SearchOption.AllDirectories);

            //Check that we got every file that we need/expect
            if (newVersionFiles.LongLength != appliedVersionFiles.LongLength)
            {
                var hasMoreFiles = appliedVersionFiles.LongLength > newVersionFiles.LongLength;
                Logger.Error("There are {0} files in the applied version {1}", 
                    hasMoreFiles ? "more" : "less",
                    hasMoreFiles ? 
                        $", files that exist that shouldn't exist:\r\n* {string.Join("\r\n* ", appliedVersionFiles.Except(newVersionFiles))}" :
                        $", files that should exist:\r\n* {string.Join("\r\n* ", newVersionFiles.Except(appliedVersionFiles))}");
                return false;
            }
            
            Logger.WriteLine("Cross checking files");
            double filesCheckedCount = 0;
            using var checkProgressBar = new ProgressBar();

            //Check that the files are bit-for-bit and that the folder structure is the same
            foreach (var file in newVersionFiles)
            {
                var fileName = file.Remove(0, Global.NewVersionLocation.Length);
                var fileIndex = appliedVersionFiles.IndexOf(x => x?.EndsWith(fileName) ?? false);

                await using var fileStream = File.OpenRead(appliedVersionFiles[fileIndex]);
                await using var newVersionStream = File.OpenRead(file);

                //See if the file lengths are the same
                if (fileStream.Length != newVersionStream.Length)
                {
                    Logger.Error("File contents of {0} is not the same", fileName);
                    return false;
                }

                //Check files bit-for-bit
                while (true)
                {
                    var fileBit = fileStream.ReadByte();
                    var newFileBit = newVersionStream.ReadByte();

                    if (fileBit != newFileBit)
                    {
                        Logger.Error("File contents of {0} is not the same", fileName);
                        return false;
                    }

                    //We hit the end of the file without any bit being different, break out
                    if (fileBit == -1)
                    {
                        break;
                    }
                }

                filesCheckedCount++;
                checkProgressBar.Report(filesCheckedCount / newVersionFiles.LongLength);
            }
            
            return true;
        }
    }
}