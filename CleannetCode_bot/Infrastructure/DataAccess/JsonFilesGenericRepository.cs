using CleannetCode_bot.Infrastructure.DataAccess.Interfaces;
using CleannetCode_bot.Infrastructure.Multithreading.Extensions;
using CleannetCode_bot.Infrastructure.Multithreading.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CleannetCode_bot.Infrastructure.DataAccess;

public class JsonFilesGenericRepository<TKey, TEntity> : IGenericRepository<TKey, TEntity> where TKey : IEquatable<TKey>
{
    private readonly Lazy<string> _entityTypeName = new(() => typeof(TEntity).Name);
    private readonly Lazy<string> _keyTypeName = new(() => typeof(TKey).Name);
    private readonly ILockService<TKey, JsonFilesGenericRepository<TKey, TEntity>> _lockService;
    private readonly ILogger<JsonFilesGenericRepository<TKey, TEntity>> _logger;
    private readonly IOptionsMonitor<JsonFilesGenericRepositoryOptions<TKey, TEntity>> _optionsMonitor;
    private string _basePathCreatedName = string.Empty;

    public JsonFilesGenericRepository(
        ILogger<JsonFilesGenericRepository<TKey, TEntity>> logger,
        ILockService<TKey, JsonFilesGenericRepository<TKey, TEntity>> lockService,
        IOptionsMonitor<JsonFilesGenericRepositoryOptions<TKey, TEntity>> optionsMonitor)
    {
        _logger = logger;
        _lockService = lockService;
        _optionsMonitor = optionsMonitor;
    }

    private JsonFilesGenericRepositoryOptions<TKey, TEntity> Options => _optionsMonitor.CurrentValue;

    public async Task SaveAsync(TKey key, TEntity entity, CancellationToken cancellationToken = default)
    {
        await _lockService.LockAsync(
            key: key,
            action: async () =>
            {
                CreateDirectory();
                var fileName = Options.GetFilePath(key: key);
                await using var fileStream = File.OpenWrite(path: fileName);
                fileStream.SetLength(value: 0);
                await JsonSerializer.SerializeAsync(
                    utf8Json: fileStream,
                    value: entity,
                    options: Options.JsonSerializerOptions,
                    cancellationToken: cancellationToken);
                _logger.LogDebug(
                    message: "Saved with {FileName} with key {TKey} {Key} entity {TEntity} {Entity}",
                    fileName, _keyTypeName, key, _entityTypeName, entity);
                return false;
            },
            cancellationToken: cancellationToken);
    }

    public async Task<TEntity?> ReadAsync(TKey key, CancellationToken cancellationToken = default)
    {
        return await _lockService.LockAsync(
            key: key,
            action: async () =>
            {
                CreateDirectory();
                var fileName = Options.GetFilePath(key: key);
                if (!File.Exists(path: fileName))
                {
                    _logger.LogDebug(
                        message: "Not found entity with {FileName} with key {TKey} {Key} of type entity {TEntity}",
                        fileName, _keyTypeName, key, _entityTypeName);
                    return default;
                }

                await using var fileStream = File.OpenRead(path: fileName);
                TEntity? entity;
                try
                {
                    entity = await JsonSerializer.DeserializeAsync<TEntity>(
                        utf8Json: fileStream,
                        options: Options.JsonSerializerOptions,
                        cancellationToken: cancellationToken);
                }
                catch (JsonException jsonException)
                {
                    _logger.LogCritical(
                        exception: jsonException,
                        message: "Json file is invalid {FileName} of with key {TKey} {Key} entity {TEntity}",
                        fileName, _keyTypeName, key, _entityTypeName);
                    return default;
                }
                _logger.LogDebug(
                    message: "Read entity with {FileName} with key {TKey} {Key} entity {TEntity} {Entity}",
                    fileName, _keyTypeName, key, _entityTypeName, entity);
                return entity;
            },
            cancellationToken: cancellationToken);
    }

    public async Task RemoveAsync(TKey key, CancellationToken cancellationToken = default)
    {
        await _lockService.LockAsync(
            key: key,
            action: () =>
            {
                CreateDirectory();
                var fileName = Options.GetFilePath(key: key);
                if (!File.Exists(path: fileName))
                {
                    _logger.LogDebug(
                        message: "Not found entity with {FileName} with key {TKey} {Key} of type entity {TEntity}",
                        fileName,
                        _keyTypeName,
                        key,
                        _entityTypeName);
                    return Task.FromResult(false);
                }
                File.Delete(fileName);
                return Task.FromResult(false);
            },
            cancellationToken: cancellationToken);
    }

    private void CreateDirectory()
    {
        if (ReferenceEquals(objA: _basePathCreatedName, objB: Options.BasePath)) return;
        if (!Directory.Exists(path: Options.BasePath))
            Directory.CreateDirectory(path: Options.BasePath);
        _basePathCreatedName = Options.BasePath;
    }
}