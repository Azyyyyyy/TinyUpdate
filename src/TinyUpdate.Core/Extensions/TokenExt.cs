using System;
using System.Threading;
using System.Threading.Tasks;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Core.Extensions
{
    /// <summary>
    /// Extensions to help with processing <see cref="CancellationToken"/>'s
    /// </summary>
    public static class TokenExt
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(TokenExt));

        /// <summary>
        /// Waits for this <see cref="CancellationToken"/> to be cancelled
        /// </summary>
        /// <param name="tokenSource">Token to use for waiting</param>
        public static async Task Wait(this CancellationToken tokenSource)
        {
            try
            {
                await Task.Delay(-1, tokenSource);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}