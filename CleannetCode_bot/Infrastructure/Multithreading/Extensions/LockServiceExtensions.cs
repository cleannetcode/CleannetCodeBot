using CleannetCode_bot.Infrastructure.Multithreading.Interfaces;

namespace CleannetCode_bot.Infrastructure.Multithreading.Extensions;

public static class LockServiceExtensions
{
    public static async Task<T> LockAsync<T, TKey, TCategory>(
        this ILockService<TKey, TCategory> lockService,
        TKey key,
        Func<Task<T>> action,
        CancellationToken cancellationToken = default) where TKey : IEquatable<TKey>
    {
        await lockService.WaitAsync(key: key, cancellationToken: cancellationToken);
        try { return await action(); }
        finally { await lockService.ReleaseAsync(key: key, cancellationToken: cancellationToken); }
    }
}