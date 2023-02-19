namespace CleannetCode_bot.Features.Statistics;

public interface IStorageService
{
    Task AddObject(object obj, Type type, string methodName, CancellationToken cts);
}