using System.Runtime.CompilerServices;

namespace CleannetCode_bot.Features.Statistics;

public interface IGenericStorageService
{
    Task AddObjectAsync<T>(T obj, string methodName, CancellationToken cancellationToken = default);
}