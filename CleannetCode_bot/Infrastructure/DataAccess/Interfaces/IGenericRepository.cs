namespace CleannetCode_bot.Infrastructure.DataAccess.Interfaces;

public interface IGenericRepository<in TKey, TEntity> where TKey : IEquatable<TKey>
{
    Task SaveAsync(TKey key, TEntity entity, CancellationToken cancellationToken = default);

    Task<TEntity?> ReadAsync(TKey key, CancellationToken cancellationToken = default);

    Task RemoveAsync(TKey key, CancellationToken cancellationToken = default);
}