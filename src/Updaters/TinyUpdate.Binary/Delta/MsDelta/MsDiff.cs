using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Helper;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Temporary;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Binary.Delta.MsDelta
{
    public class MsDiff : IDeltaUpdate
    {
        private readonly ILogging _logger = LoggingCreator.CreateLogger(nameof(MsDiff));

        /// <inheritdoc cref="IDeltaUpdate.Extension"/>
        public string Extension => ".diff";

        /// <inheritdoc cref="ISpecificOs.IntendedOs"/>
        public OSPlatform? IntendedOs => OSPlatform.Windows;

        /// <inheritdoc cref="IDeltaUpdate.CreateDeltaFile"/>
        public bool CreateDeltaFile(
            TemporaryFolder tempFolder,
            string originalFileLocation,
            string newFileLocation,
            string deltaFileLocation,
            out Stream? deltaFileStream,
            Action<double>? progress = null)
        {
            deltaFileStream = null;
            if (OSHelper.ActiveOS != OSPlatform.Windows)
            {
                _logger.Error("We aren't on Windows so can't apply MSDiff update");
                return false;
            }

            return CreateDelta(FileTypeSet.ExecutablesLatest, GetCreateFlags(), CreateFlags.None, originalFileLocation,
                newFileLocation, null, null, new DeltaInput(), IntPtr.Zero, HashAlgId.Crc32, deltaFileLocation);
        }

        /// <inheritdoc cref="IDeltaUpdate.ApplyDeltaFile"/>
        public async Task<bool> ApplyDeltaFile(
            TemporaryFolder tempFolder,
            string originalFileLocation,
            string newFileLocation,
            string deltaFileName,
            Stream deltaFileStream,
            Action<double>? progress = null)
        {
            if (OSHelper.ActiveOS != OSPlatform.Windows)
            {
                _logger.Error("We aren't on Windows so can't apply MSDiff update");
                return false;
            }
            if (string.IsNullOrWhiteSpace(deltaFileName))
            {
                _logger.Error("Wasn't given a file location for the delta file");
                return false;
            }

            //Put the delta file onto disk
            using var tmpDeltaFile = tempFolder.CreateTemporaryFile(deltaFileName);
            var tmpFileStream = tmpDeltaFile.GetStream(preallocationSize: deltaFileStream.Length);
            await deltaFileStream.CopyToAsync(tmpFileStream);
            tmpFileStream.Dispose();
            deltaFileStream.Dispose();

            //If baseFile + outputLocation are the same, copy it to a tmp file
            //and then give it that (deleting it after)
            TemporaryFile? tmpBaseFile = null;
            if (originalFileLocation == newFileLocation)
            {
                tmpBaseFile = tempFolder.CreateTemporaryFile(Path.GetRandomFileName());
                File.Copy(originalFileLocation, tmpBaseFile.Location);
                originalFileLocation = tmpBaseFile.Location;
            }

            //Make the updated file!
            File.Create(newFileLocation).Dispose();
            var wasApplySuccessful = ApplyDelta(ApplyFlags.None, originalFileLocation, tmpDeltaFile.Location, newFileLocation);
            tmpBaseFile?.Dispose();

            return wasApplySuccessful;
        }

        /// <summary>
        /// The ApplyDelta function use the specified delta and source files to create a new copy of the target file.
        /// </summary>
        /// <param name="applyFlags">Either <see cref="ApplyFlags.None"/> or <see cref="ApplyFlags.AllowLegacy"/>.</param>
        /// <param name="sourceName">The name of the source file to which the delta is to be applied.</param>
        /// <param name="deltaName">The name of the delta to be applied to the source file.</param>
        /// <param name="targetName">The name of the target file that is to be created.</param>
        [DllImport("msdelta.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ApplyDelta(
            [MarshalAs(UnmanagedType.I8)] ApplyFlags applyFlags,
            string sourceName,
            string deltaName,
            string targetName);

        /// <summary>
        /// The CreateDelta function creates a delta from the specified source and target files and write the output delta to the designated file name.
        /// </summary>
        /// <param name="fileTypeSet">The file type set used for Create.</param>
        /// <param name="setFlags">The file type set used for Create.</param>
        /// <param name="resetFlags">The file type set used for Create.</param>
        /// <param name="sourceName">The file type set used for Create.</param>
        /// <param name="targetName">The name of the target against which the source is compared.</param>
        /// <param name="sourceOptionsName">Reserved. Pass NULL.</param>
        /// <param name="targetOptionsName">Reserved. Pass NULL.</param>
        /// <param name="globalOptions">Reserved. Pass a DELTA_INPUT structure with lpStart set to NULL and uSize set to 0.</param>
        /// <param name="targetFileTime">The time stamp set on the target file after delta Apply. If NULL, the timestamp of the target file during delta Create will be used.</param>
        /// <param name="hashAlgId">ALG_ID of the algorithm to be used to generate the target signature.</param>
        /// <param name="deltaName">The name of the delta file to be created.</param>
        [DllImport("msdelta.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateDelta(
            [MarshalAs(UnmanagedType.I8)] FileTypeSet fileTypeSet,
            [MarshalAs(UnmanagedType.I8)] CreateFlags setFlags,
            [MarshalAs(UnmanagedType.I8)] CreateFlags resetFlags,
            string sourceName,
            string targetName,
            string? sourceOptionsName,
            string? targetOptionsName,
            DeltaInput globalOptions,
            IntPtr targetFileTime,
            [MarshalAs(UnmanagedType.U4)] HashAlgId hashAlgId,
            string deltaName);

        private static CreateFlags GetCreateFlags() => RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm => CreateFlags.Cli4Arm | CreateFlags.IgnoreFileSizeLimit,
            Architecture.Arm64 => CreateFlags.Cli4Arm64 | CreateFlags.IgnoreFileSizeLimit,
            Architecture.X64 => CreateFlags.Cli4Amd64 | CreateFlags.IgnoreFileSizeLimit,
            Architecture.X86 => CreateFlags.Cli4I386 | CreateFlags.IgnoreFileSizeLimit,
            _ => CreateFlags.IgnoreFileSizeLimit
        };
    }
}