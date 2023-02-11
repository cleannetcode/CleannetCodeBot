using Microsoft.Extensions.Logging;

namespace CleannetCode_bot.Infrastructure.Multithreading.Interfaces;

/// <summary>
///     Lock service
/// </summary>
/// <typeparam name="TKey">Type of lock key</typeparam>
/// <typeparam name="TCategory">
///     Type of lock category. May be used in realization. Used as category in
///     <see cref="ILogger{TCategoryName}" />
/// </typeparam>
// ReSharper disable once UnusedTypeParameter
public interface ILockService<in TKey, out TCategory> where TKey : IEquatable<TKey>
{
    Task WaitAsync(TKey key, CancellationToken cancellationToken = default);

    Task ReleaseAsync(TKey key, CancellationToken cancellationToken = default);
}