using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Logging;
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
            string tempFolder,
            string originalFile,
            string newFile,
            string deltaFile,
            Stream? deltaStream,
            Action<decimal>? progress = null)
        {
            Stream? inputStream = null;
            /*If this is the same file then we
             want to copy it to mem and not read from disk*/
            if (deltaFile == originalFile)
            {
                inputStream = new MemoryStream();
                var fileStream = StreamUtil.SafeOpenRead(originalFile);
                if (fileStream == null)
                {
                    _logger.Error("Wasn't able to grab {0} for applying a BSDiff update", originalFile);
                    return false;
                }

                await fileStream.CopyToAsync(inputStream);
                fileStream.Dispose();
                inputStream.Seek(0, SeekOrigin.Begin);
            }

            //Create streams for old file and where the new file will be
            var outputStream = File.OpenWrite(newFile);
            inputStream ??= StreamUtil.SafeOpenRead(originalFile);

            //Check streams that can be null
            if (inputStream == null)
            {
                _logger.Error("Wasn't able to grab {0} for applying a BSDiff update", originalFile);
                return false;
            }

            if (deltaStream == null)
            {
                _logger.Error("fileEntry doesn't have a stream, can't make BSDiff update");
                return false;
            }

            //Create a memory stream as we really need to be able to seek
            var patchMemStream = new MemoryStream();
            await deltaStream.CopyToAsync(patchMemStream);
            var successfulUpdate = await BinaryPatchUtility.Apply(inputStream, () =>
            {
                //Copy the files over in a memory stream
                var memStream = new MemoryStream();
                patchMemStream.Seek(0, SeekOrigin.Begin);
                patchMemStream.CopyTo(memStream);
                memStream.Seek(0, SeekOrigin.Begin);

                return memStream;
            }, outputStream, progress);

            outputStream.Dispose();
            inputStream.Dispose();
            return successfulUpdate;
        }

        /// <inheritdoc cref="IDeltaUpdate.CreateDeltaFile"/>
        public bool CreateDeltaFile
        (string baseFileLocation,
            string tempFolder,
            string newFileLocation,
            string deltaFileLocation,
            out Stream? deltaFileStream,
            Action<decimal>? progress = null)
        {
            var tmpDeltaFileStream = new MemoryStream();

            var success = BinaryPatchUtility.Create(
                File.ReadAllBytes(baseFileLocation),
                File.ReadAllBytes(newFileLocation),
                tmpDeltaFileStream, progress);
            deltaFileStream = tmpDeltaFileStream;

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