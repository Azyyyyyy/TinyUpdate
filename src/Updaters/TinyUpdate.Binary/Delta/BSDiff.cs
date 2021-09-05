using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Helper;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Temporary;
using TinyUpdate.Core.Update;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Binary.Delta
{
    /// <summary>
    /// Processes creating and applying files with <see cref="BinaryPatchUtility"/>
    /// </summary>
    public class BSDiff : IDeltaUpdate
    {
        private readonly ILogging _logger = LoggingCreator.CreateLogger(nameof(MsDelta));

        /// <inheritdoc cref="IDeltaUpdate.Extension"/>
        public string Extension => ".bsdiff";

        /// <inheritdoc cref="ISpecificOs.IntendedOs"/>
        public OSPlatform? IntendedOs => null;

        /// <inheritdoc cref="IDeltaUpdate.ApplyDeltaFile"/>
        public async Task<bool> ApplyDeltaFile(
            TemporaryFolder tempFolder,
            string originalFileLocation,
            string newFileLocation,
            string deltaFileName,
            Stream deltaFileStream,
            Action<double>? progress = null)
        {
            Stream? inputStream = null;
            /*If this is the same file then we
             want to copy it to mem and not read from disk*/
            if (deltaFileName == originalFileLocation)
            {
                inputStream = new MemoryStream();
                using var fileStream = StreamUtil.SafeOpenRead(originalFileLocation);
                if (fileStream == null)
                {
                    _logger.Error("Wasn't able to grab {0} for applying a BSDiff update", originalFileLocation);
                    return false;
                }

                await fileStream.CopyToAsync(inputStream);
                inputStream.Seek(0, SeekOrigin.Begin);
            }

            inputStream ??= StreamUtil.SafeOpenRead(originalFileLocation);
            if (inputStream == null)
            {
                _logger.Error("Wasn't able to grab {0} for applying a BSDiff update", originalFileLocation);
                return false;
            }

            //Create a memory stream if seeking is not possible as that is needed
            if (!deltaFileStream.CanSeek)
            {
                var patchMemStream = new MemoryStream();
                await deltaFileStream.CopyToAsync(patchMemStream);
                deltaFileStream.Dispose();
                deltaFileStream = patchMemStream;
            }

            var outputStream = FileHelper.OpenWrite(newFileLocation, inputStream.Length);
            var successfulUpdate = await BinaryPatchUtility.Apply(inputStream, () =>
            {
                //Copy the files over in a memory stream
                var memStream = new MemoryStream();
                deltaFileStream.Seek(0, SeekOrigin.Begin);
                deltaFileStream.CopyTo(memStream);
                memStream.Seek(0, SeekOrigin.Begin);

                return memStream;
            }, outputStream, progress);

            outputStream.Dispose();
            inputStream.Dispose();
            deltaFileStream.Dispose();
            return successfulUpdate;
        }

        /// <inheritdoc cref="IDeltaUpdate.CreateDeltaFile"/>
        public bool CreateDeltaFile(
            TemporaryFolder tempFolder,
            string originalFileLocation,
            string newFileLocation,
            string deltaFileLocation,
            out Stream? deltaFileStream,
            Action<double>? progress = null)
        {
            deltaFileStream = new MemoryStream();
            var success = BinaryPatchUtility.Create(
                File.ReadAllBytes(originalFileLocation),
                File.ReadAllBytes(newFileLocation),
                deltaFileStream, progress);

            if (!success)
            {
                deltaFileStream.Dispose();
                deltaFileStream = null;
            }

            deltaFileStream?.Seek(0, SeekOrigin.Begin);
            return success;
        }
    }
}