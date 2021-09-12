using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TinyUpdate.Binary.Extensions;
using TinyUpdate.Core.Helper;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Temporary;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Binary.Delta
{
    /// <summary>
    /// Creates a delta file for two versions of a file 
    /// </summary>
    public static class DeltaCreation
    {
        private static readonly ILogging Logging = LoggingCreator.CreateLogger(nameof(DeltaCreation));
        private static readonly Dictionary<OSPlatform, IReadOnlyList<IDeltaUpdate>> CachedUpdaters = new Dictionary<OSPlatform, IReadOnlyList<IDeltaUpdate>>();

        /// <summary>
        /// Creates a delta file by going through the different ways of creating delta files and grabbing the one that made the smallest file
        /// </summary>
        /// <param name="tempFolder">Where the temp folder is located</param>
        /// <param name="baseFileLocation">Where the older version of the file exists</param>
        /// <param name="newFileLocation">Where the newer version of the file exists</param>
        /// <param name="deltaFileLocation">Where the delta file should be stored (If we are unable to store it in a stream)</param>
        /// <param name="intendedOs">What OS this delta file will be intended for</param>
        /// <param name="extension">Extension of the delta file</param>
        /// <param name="deltaFileStream">The contents of the delta file</param>
        /// <param name="progress">Progress of making the delta file (If we can report the progress back)</param>
        public static bool CreateDeltaFile(
            TemporaryFolder tempFolder,
            string baseFileLocation,
            string newFileLocation,
            string deltaFileLocation,
            OSPlatform? intendedOs,
            out string extension,
            out Stream? deltaFileStream,
            Action<double>? progress = null)
        {
            var os = intendedOs ?? OSHelper.ActiveOS;
            IReadOnlyList<IDeltaUpdate>? deltaUpdaters = null;
            if (CachedUpdaters.ContainsKey(os))
            {
                deltaUpdaters = CachedUpdaters[os];
            }
            else
            {
                deltaUpdaters ??= DeltaUpdaters.GetUpdatersBasedOnOS(intendedOs ?? OSHelper.ActiveOS);
                CachedUpdaters.Add(os, deltaUpdaters);
            }
            var updaterCount = deltaUpdaters.Count;

            var progresses = new double[updaterCount];
            var deltaResults = new List<DeltaCreationResult>(updaterCount);

            Parallel.For(0, updaterCount, i =>
            {
                var deltaUpdater = deltaUpdaters[i];
                var deltaFile = tempFolder.CreateTemporaryFile();
                
                var successful = deltaUpdater.CreateDeltaFile(
                    tempFolder,
                    baseFileLocation,
                    newFileLocation,
                    deltaFile.Location,
                    out var stream,
                    pro =>
                    {
                        progresses[i] = pro;
                        progress?.Invoke(progresses.Sum() / updaterCount);
                    });

                if (successful)
                {
                    deltaResults.Add(
                        new DeltaCreationResult(deltaUpdater.Extension, deltaFile, stream));
                }
                else
                {
                    Logging.Warning("{0} was unable to make a delta file for {1}", 
                        nameof(deltaUpdater), baseFileLocation.GetRelativePath(newFileLocation));
                    deltaFile.Dispose();
                }
                
                if (progresses[i] < 1d)
                {
                    progresses[i] = 1d;
                    progress?.Invoke(progresses.Sum() / updaterCount);
                }
            });

            //Fail if we was unable to make any
            if (!deltaResults.Any())
            {
                extension = string.Empty;
                deltaFileStream = null;
                return false;
            }
            
            //Get the smallest delta file
            var deltaResult = deltaResults.OrderBy(x =>
            {
                long? l = null;
                if (File.Exists(x.DeltaFileLocation?.Location))
                {
                    l = new FileInfo(x.DeltaFileLocation?.Location).Length;
                }

                return l ?? x.Stream?.Length;
            }).First();
            
            //Dispose of the other delta files
            deltaResults.Remove(deltaResult);
            deltaResults.ForEach(x =>
            {
                x.Stream?.Dispose();
                x.DeltaFileLocation?.Dispose();
            });

            Logging.Information("{0} was the best option for a delta update, returning the data from it", deltaResult.Extension);
            extension = deltaResult.Extension;
            deltaFileStream = deltaResult.Stream;

            //If the delta made a file then make sure to move it
            if (File.Exists(deltaResult.DeltaFileLocation?.Location))
            {
                File.Move(deltaResult.DeltaFileLocation?.Location!, deltaFileLocation);
            }
            return true;
        }
    }

    internal class DeltaCreationResult
    {
        internal DeltaCreationResult(string extension, TemporaryFile? deltaFileLocation, Stream? stream)
        {
            Extension = extension;
            DeltaFileLocation = deltaFileLocation;
            Stream = stream;
        }
        
        public Stream? Stream { get; }

        public string Extension { get; }

        public TemporaryFile? DeltaFileLocation { get; }
    }
}