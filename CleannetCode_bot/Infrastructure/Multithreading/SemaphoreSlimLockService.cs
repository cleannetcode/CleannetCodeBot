using CleannetCode_bot.Infrastructure.Multithreading.Interfaces;
using System.Collections.Concurrent;

namespace CleannetCode_bot.Infrastructure.Multithreading;

public class SemaphoreSlimLockService<TKey, TCategory> : ILockService<TKey, TCategory> where TKey : IEquatable<TKey>
{
    private readonly ConcurrentDictionary<TKey, SemaphoreSlim> _semaphoreSlims = new();

    public async Task WaitAsync(TKey key, CancellationToken cancellationToken = default)
    {
        var semaphore = _semaphoreSlims.GetOrAdd(key: key, value: new(initialCount: 1, maxCount: 1));
        try { await semaphore.WaitAsync(cancellationToken); }
        catch (OperationCanceledException exception)
        {
            throw new OperationCanceledException(
                message:
                $"SemaphoreSlimLockService.WaitAsync is canceled before it lock key {key} of type {typeof(TKey)} on category {typeof(TCategory)}",
                innerException: exception);
        }
    }

    public Task ReleaseAsync(TKey key, CancellationToken cancellationToken = default)
    {
        var semaphore = _semaphoreSlims.GetOrAdd(key: key, value: new(initialCount: 1, maxCount: 1));
        semaphore.Release();
        return Task.CompletedTask;
    }
}