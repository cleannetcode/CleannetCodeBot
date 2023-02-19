using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Extensions.Logging;

namespace CleannetCode_bot.Features.Statistics;

public class StorageFileService : IStorageService
{
    private readonly ILogger<StorageFileService> logger;
    private static readonly string _directory = "./FileStorage/";
    private static readonly string _fileName = "data.json";

    private readonly JsonSerializerOptions optionsJson = new();

    public StorageFileService(ILogger<StorageFileService> logger)
    {
        this.logger = logger;

        optionsJson = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            WriteIndented = true
        };
    }
    public Task AddObject(object obj, Type type, string methodName, CancellationToken cts)
    {
        if (obj is null) return Task.CompletedTask;
        if (type is null) return Task.CompletedTask;
        if (methodName is null) return Task.CompletedTask;

        var json = JsonSerializer.Serialize(obj, type, optionsJson);

        string dir = $"{_directory}{methodName}/";
        Directory.CreateDirectory(dir);
        string fileName = DateTime.Now.ToString("ddMMyyyy_HHmmss_ffff") + "-" + _fileName;
        string path = Path.Combine(dir, fileName);
        File.WriteAllTextAsync(path, json, cts);

        logger.LogInformation("{DateTime:dd.MM.yyyy HH:mm:ss:ffff}\tnew file created\t{dir}\t{fileName}", DateTime.Now, dir, fileName);

        return Task.CompletedTask;
    }
}