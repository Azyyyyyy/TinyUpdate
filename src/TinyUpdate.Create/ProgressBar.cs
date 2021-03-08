using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

//Adapted from https://gist.github.com/DanielSWolf/0ab6a96899cc5377bf54
namespace TinyUpdate.Create
{
    /// <summary>
    /// An ASCII progress bar
    /// </summary>
    public class ProgressBar : IDisposable, IProgress<double>
    {
        private const int BlockCount = 10;
        private readonly TimeSpan _animationInterval = TimeSpan.FromSeconds(1.0 / 8);
        private const string Animation = @"|/-\";

        private readonly Timer _timer;

        private double _currentProgress;
        private bool _disposed;
        private int _animationIndex;

        public ProgressBar()
        {
            _timer = new Timer(TimerHandler);

            // A progress bar is only for temporary display in a console window.
            // If the console output is redirected to a file, draw nothing.
            // Otherwise, we'll end up with a lot of garbage in the target file.
            if (!Console.IsOutputRedirected)
            {
                ResetTimer();
            }
        }

        public void Report(double value)
        {
            // Make sure value is in [0..1] range
            value = Math.Max(0, Math.Min(1, value));
            Interlocked.Exchange(ref _currentProgress, value);
        }

        private void TimerHandler(object? state)
        {
            lock (_timer)
            {
                if (_disposed)
                {
                    return;
                }

                int progressBlockCount = (int) (_currentProgress * BlockCount);

                if (progressBlockCount < 0)
                {
                    Debugger.Break();
                }
                
                int percent = (int) (_currentProgress * 100);
                string text =
                    $"[{new string('#', progressBlockCount)}{new string('-', BlockCount - progressBlockCount)}] {percent,3}% {Animation[_animationIndex++ % Animation.Length]}";
                UpdateText(text);

                ResetTimer();
            }
        }

        private void UpdateText(string text)
        {
            // Backtrack to the first differing character
            StringBuilder outputBuilder = new StringBuilder();

            // If the new text is shorter than the old one: delete overlapping characters
            int overlapCount = Console.CursorLeft - text.Length;
            if (overlapCount > 0)
            {
                outputBuilder.Append(' ', overlapCount);
                outputBuilder.Append('\b', overlapCount);
            }

            Console.CursorLeft = 0;
            Console.Write(text);
        }

        private void ResetTimer()
        {
            _timer.Change(_animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        public void Dispose()
        {
            lock (_timer)
            {
                _disposed = true;
                UpdateText(string.Empty);
            }
        }
    }
}