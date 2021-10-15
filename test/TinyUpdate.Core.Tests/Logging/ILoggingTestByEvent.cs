using System;
using System.Threading;
using System.Threading.Tasks;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Core.Tests.Logging
{
    // ReSharper disable once InconsistentNaming
    public abstract class ILoggingTestByEvent<TLogger, TBuilder> : ILoggingTest<TLogger, TBuilder>
        where TLogger : ILogging
        where TBuilder : LoggingBuilder
    {
        protected ILoggingTestByEvent(TBuilder builder) : base(builder)
        { }

        protected override async Task<bool> DoesLogOutput(Action action)
        {
            return !string.IsNullOrWhiteSpace(await WaitForLog(action));
        }

        private async Task Wait(CancellationTokenSource tokenS, int time = -1)
        {
            try
            {
                if (!tokenS.IsCancellationRequested)
                {
                    await Task.Delay(time, tokenS.Token);
                }
            }
            catch (TaskCanceledException) { }
        }

        protected EventHandler<string>? NewOutput;
        
        protected override async Task<string> WaitForLog(Action? action = null)
        {
            var s = string.Empty;
            var tokenS = new CancellationTokenSource();
            var hasChanged = false;
            NewOutput += (sender, st) =>
            {
                s = st;
                hasChanged = true;
                tokenS.Cancel(false);
            };
            action?.Invoke();

            if (hasChanged)
            {
                await Wait(tokenS, 10);
            }
            
            return s;
        }
    }
}