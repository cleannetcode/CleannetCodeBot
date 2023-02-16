using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Extensions.Logging;

namespace CleannetCode_bot.Features.Statistics;

public class StorageFileService : IGenericStorageService
{
    private readonly ILogger<StorageFileService> _logger;
    private const string Directory = "./FileStorage/";
    private const string FileSuffix = "data.json";

    private readonly JsonSerializerOptions _optionsJson = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), WriteIndented = true
    };

    public StorageFileService(ILogger<StorageFileService> logger)
    {
        _logger = logger;
    }

    private static async Task<string> SaveJsonAsync(string methodName, CancellationToken cts, string json)
    {
        var dir = $"{Directory}{methodName}/";
        System.IO.Directory.CreateDirectory(dir);
        var fileName = $"{DateTime.Now:ddMMyyyy_HHmmss_ffff}-{FileSuffix}";
        var path = Path.Combine(dir, fileName);
        await File.WriteAllTextAsync(path, json, cts);
        return path;
    }

    public async Task AddObjectAsync<T>(T obj, string methodName, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(obj, _optionsJson);
        var path = await SaveJsonAsync(methodName, cancellationToken, json);
        _logger.LogInformation("New file created {Path}", path);
    }
}