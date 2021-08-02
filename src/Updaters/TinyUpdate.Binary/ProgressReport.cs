using System;
using System.Threading;

namespace TinyUpdate.Binary
{
    public class ProgressReport
    {
        private int _filesProcessed;
        private readonly double _fileCount;
        private readonly Action<double>? _action;
        private readonly object _actionLock = new();

        /// <summary>
        /// Creates a <see cref="ProgressReport"/>
        /// </summary>
        /// <param name="fileCount">How many files we need to go through</param>
        /// <param name="action">The action to call when we need to report back progress</param>
        public ProgressReport(int fileCount, Action<double>? action)
        {
            _fileCount = fileCount + 2;
            _action = action;
        }

        /// <summary>
        /// Reports when we have progressed on processing a file
        /// </summary>
        /// <param name="progress">How much we have progressed in processing file</param>
        public void PartialProcessedFile(double progress)
        {
            lock (_actionLock)
            {
                _action?.Invoke((progress + _filesProcessed) / _fileCount);
            }
        }

        /// <summary>
        /// Reports when we have fully processed a file
        /// </summary>
        public void ProcessedFile()
        {
            Interlocked.Increment(ref _filesProcessed);
            lock (_actionLock)
            {
                _action?.Invoke(_filesProcessed / _fileCount);
            }
        }

        /// <summary>
        /// Reports when we are done with cleanup
        /// </summary>
        public void DoneCleanup()
        {
            // ReSharper disable once InconsistentlySynchronizedField
            _action?.Invoke(1);
        }
    }
}