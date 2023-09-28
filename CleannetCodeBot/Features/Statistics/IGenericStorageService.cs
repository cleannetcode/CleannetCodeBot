namespace CleannetCodeBot.Features.Statistics;

public interface IGenericStorageService
{
    Task AddObjectAsync<T>(T obj, string methodName, CancellationToken cancellationToken = default);
}