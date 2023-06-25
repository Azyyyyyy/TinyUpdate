using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TinyUpdate.Core.Extensions;

namespace TinyUpdate.Core;

public class Queuer : IDisposable
{
    private bool _disposed;
    private readonly BlockingCollection<Func<Task>> _processQueue = new();
    private readonly object _addLock = new object();
    private bool _started;
    private readonly string _threadName;

    public Queuer(string name)
    {
        _threadName = name + " Queue Thread";
    }

    public void Start()
    {
        if (_started)
        {
            return;
        }
        
        _started = true;
        new Thread(ProcessQueue) { Name = _threadName }.Start();
    }

    private async void ProcessQueue()
    {
        foreach (var func in _processQueue.GetConsumingEnumerable())
        {
            await func();
        }
    }
    
    public async Task WaitForObject(Action action, CancellationToken? waitToken = null)
    {
        TaskCompletionSource<object?> completionSource = new();
        lock (_addLock)
        {
            _processQueue.Add(() =>
            {
                action();
                completionSource.SetResult(null);
                return Task.CompletedTask;
            });

            //Wait for the token if we get one
            if (waitToken != null)
            {
                _processQueue.Add(() => waitToken.Value.Wait());
            }
        }

        await completionSource.Task;
    }
    
    public async Task<T> WaitForObject<T>(Func<Task<T>> taskFunc, CancellationToken? waitToken = null)
    {
        TaskCompletionSource<T> completionSource = new();
        lock (_addLock)
        {
            _processQueue.Add(async () =>
            {
                var task = taskFunc.Invoke();
                await task;
                ProcessTask(completionSource, task);
            });

            //Wait for the token if we get one
            if (waitToken != null)
            {
                _processQueue.Add(() => waitToken.Value.Wait());
            }
        }
        
        return await completionSource.Task;
    }
    
    public async Task<T> WaitForObject<T>(Func<T> task, CancellationToken? waitToken = null)
    {
        TaskCompletionSource<T> completionSource = new();
        lock (_addLock)
        {
            _processQueue.Add(() =>
            {
                var item = task.Invoke();
                completionSource.SetResult(item);
                return Task.CompletedTask;
            });

            //Wait for the token if we get one
            if (waitToken != null)
            {
                _processQueue.Add(() => waitToken.Value.Wait());
            }
        }

        return await completionSource.Task;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessTask<T>(TaskCompletionSource<T> completionSource, Task<T> task)
    {
        if (task.IsCanceled)
        {
            completionSource.SetCanceled();
            return;
        }
        if (task is { IsCompleted: true, IsFaulted: false })
        {
            completionSource.SetResult(task.Result);
            return;
        }
        if (task.IsFaulted)
        {
            completionSource.SetException(task.Exception ?? new Exception("Unknown fault!")); //If it's faulted then it *should* have an exception
            return;
        }

        completionSource.SetException(new Exception("Unknown task state!"));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        
        _processQueue.CompleteAdding();
        GC.SuppressFinalize(this);
    }

    ~Queuer() => Dispose();
}