﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Core
{
    /// <summary>
    /// Class that contains all the information we know from the RELEASE file
    /// </summary>
    public class ReleaseFile
    {
        private static readonly ILogging Logging = LoggingCreator.CreateLogger(nameof(ReleaseFile));
        
        public ReleaseFile(string sha256, string name, long size, Version? oldVersion)
        {
            SHA256 = sha256;
            Name = name;
            Size = size;
            OldVersion = oldVersion;
        }

        /// <summary>
        /// Hash that the downloaded file should be
        /// </summary>
        public string SHA256 { get; }

        /// <summary>
        /// The name of the release file
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The size of the file when downloaded
        /// </summary>
        public long Size { get; }

        /// <summary>
        /// The version that this update will be coming from
        /// </summary>
        public Version? OldVersion { get; }

        /// <summary>
        /// Creates a RELEASE file 
        /// </summary>
        /// <param name="releaseFiles">All the release data to put into the RELEASE file</param>
        /// <param name="fileLocation">Where the file should be located (Do not give a filename, we add it)</param>
        /// <returns></returns>
        public static async Task<bool> CreateReleaseFile(IEnumerable<ReleaseFile> releaseFiles, string fileLocation)
        {
            fileLocation = Path.Combine(fileLocation, "RELEASE");
            var file = new FileInfo(fileLocation);
            if (file.Exists)
            {
                Logging.Warning($"{fileLocation} already exists, going to delete it and recreate it");
                file.Delete();
            }

            using var textFileStream = file.CreateText();
            foreach (var releaseFile in releaseFiles)
            {
                await textFileStream.WriteLineAsync($"{releaseFile.SHA256} {releaseFile.Name} {(releaseFile.OldVersion != null ? $"{releaseFile.OldVersion} " : "")}{releaseFile.Size}");
            }

            return true;
        }

        /// <summary>
        /// Reads a chunk of lines and make them into <see cref="ReleaseFile"/>
        /// </summary>
        /// <param name="lines">Lines to make into <see cref="ReleaseFile"/></param>
        public static IEnumerable<ReleaseFile> ReadReleaseFile(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                var lineS = line.Split(' ');
                /*Check that the line only has 3/4 lines, if not then
                 that means it's not a release file for sure*/
                if (lineS.Length < 3 || lineS.Length > 4)
                {
                    continue;
                }
                
                var sha256 = lineS[0];
                var fileName = lineS[1];
                Version? oldVersion = null;
                if (long.TryParse(lineS[lineS.Length - 1], out var fileSize) 
                    && SHA256Util.IsValidSHA256(sha256)
                    && (lineS.Length != 4 || Version.TryParse(lineS[2], out oldVersion)))
                {
                    yield return new ReleaseFile(sha256, fileName, fileSize, oldVersion);
                }
                
                //If we got here then we wasn't able to create a release file from the data given
                Logging.Warning($"Line {line} is not a valid ReleaseFile");
            }
        }
    }
}