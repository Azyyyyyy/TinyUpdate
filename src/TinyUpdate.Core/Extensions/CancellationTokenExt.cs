using System.Threading;
using System.Threading.Tasks;

namespace TinyUpdate.Core.Extensions;

public static class CancellationTokenExt
{
    public static async Task Wait(this CancellationToken token, int timeout = -1)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }
        try
        {
            await Task.Delay(timeout, token);
        }
        catch (TaskCanceledException) { }
    }
}