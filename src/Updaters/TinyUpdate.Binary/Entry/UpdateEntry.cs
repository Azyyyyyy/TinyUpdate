using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TinyUpdate.Binary.Extensions;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Binary.Entry
{
    /// <summary>
    /// An entry with all of the files that are in a update
    /// </summary>
    public class UpdateEntry
    {
        private readonly ILogging _logger = LoggingCreator.CreateLogger(nameof(UpdateEntry));

        public UpdateEntry(IEnumerable<FileEntry> fileEntries)
        {
            foreach (var fileEntry in fileEntries)
            {
                ProcessFileEntry(fileEntry);
            }
        }

        public UpdateEntry()
        {
            _logger.Warning(
                "UpdateEntry has been created with nothing passed in. Only do this if you plan to load in a IAsyncEnumerable instead of a IEnumerable");
        }

        /// <summary>
        /// Loads everything from a <see cref="IAsyncEnumerable{T}"/> into this <see cref="UpdateEntry"/>
        /// </summary>
        /// <param name="fileEntries">File entries to load in</param>
        public async Task LoadAsyncEnumerable(IAsyncEnumerable<FileEntry?> fileEntries)
        {
            await foreach (var fileEntry in fileEntries)
            {
                /*If the fileEntry is null then something happened while grabbing*
                 the update files, clear out*/
                if (fileEntry == null)
                {
                    DeltaFile.Clear();
                    SameFile.Clear();
                    NewFile.Clear();
                    break;
                }

                ProcessFileEntry(fileEntry);
            }
        }

        private void ProcessFileEntry(FileEntry fileEntry)
        {
            //Get rid of any streams that will be empty
            if (fileEntry.Filesize == 0)
            {
                fileEntry.Stream?.Dispose();
                fileEntry.Stream = null;
            }

            //Add to the correct 
            if (fileEntry.IsDeltaFile())
            {
                DeltaFile.Add(fileEntry);
                return;
            }

            if (fileEntry.IsNewFile())
            {
                NewFile.Add(fileEntry);
                return;
            }

            if (fileEntry.DeltaExtension.EndsWith("load"))
            {
                LoaderFile = fileEntry;
                return;
            }

            SameFile.Add(fileEntry);
        }

        /// <summary>
        /// Files that need to be processed as a delta file
        /// </summary>
        public List<FileEntry> DeltaFile { get; } = new();

        /// <summary>
        /// Files that should already be on the device
        /// </summary>
        public List<FileEntry> SameFile { get; } = new();

        /// <summary>
        /// Files that aren't in the last update 
        /// </summary>
        public List<FileEntry> NewFile { get; } = new();

        /// <summary>
        /// File that loads up the application
        /// </summary>
        public FileEntry? LoaderFile { get; private set; }

        /// <summary>
        /// Every file that we contain in this update
        /// </summary>
        public IEnumerable<FileEntry> All => DeltaFile.Concat(SameFile).Concat(NewFile);

        /// <summary>
        /// The amount of files that we have
        /// </summary>
        public int Count => DeltaFile.Count + SameFile.Count + NewFile.Count;
    }
}