using System;
using System.Threading;

//Adapted from https://gist.github.com/DanielSWolf/0ab6a96899cc5377bf54
namespace TinyUpdate.Create
{
    /// <summary>
    /// An ASCII progress bar
    /// </summary>
    public class ProgressBar : IDisposable, IProgress<double>
    {
        private readonly CustomConsoleLogger _logger = new(nameof(ProgressBar));
        private readonly TimeSpan _animationInterval = TimeSpan.FromSeconds(1.0 / 8);
        private const string Animation = @"|/-\";

        private readonly Timer _timer;

        private double _currentProgress;
        private bool _disposed;
        private int _animationIndex;
        private string _currentText = "";

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

                var blockCount = Console.WindowWidth / 2;
                int progressBlockCount = (int) (_currentProgress * blockCount);

                int percent = (int) (_currentProgress * 100);
                string text =
                    $"[{new string('#', progressBlockCount)}{new string('-', blockCount - progressBlockCount)}] {percent,3}% {Animation[_animationIndex++ % Animation.Length]}";
                UpdateText(text);

                ResetTimer();
            }
        }

        private void UpdateText(string text)
        {
            if (Console.CursorLeft > _currentText.Length)
            {
                _logger.WriteLine();
            }

            // If the new text is shorter than the old one: delete overlapping characters
            int overlapCount = _currentText.Length - text.Length;
            if (Console.CursorLeft != 0 && overlapCount > 0)
            {
                _logger.Write(new string(' ', overlapCount));
                _logger.Write(new string('\b', overlapCount));
            }

            Console.CursorLeft = 0;
            _logger.Write(text);
            _currentText = text;
        }

        private void ResetTimer()
        {
            _timer.Change(_animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        public void Dispose()
        {
            lock (_timer)
            {
                TimerHandler(null);
                _disposed = true;
            }
        }
    }
}