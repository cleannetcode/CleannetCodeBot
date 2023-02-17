using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace CleannetCode_bot.Features.Homework;

public class HomeworkStorageRepository<T> where T : new()
{
    private readonly string _fileName;
    private static string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;

    public HomeworkStorageRepository(string filename)
    {
        _fileName = filename;
    }

    public async Task<bool> Save(T dataObject)
    {
        if (dataObject == null)
            return false;

        var json = JsonSerializer.Serialize(dataObject, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        if (String.IsNullOrWhiteSpace(json))
            return false;

        var path = Path.Combine(BaseDirectory, _fileName);
        await File.WriteAllTextAsync(path, json);

        return true;
    }

    public async Task<T> Get()
    {
        var result = new T();
        var path = Path.Combine(BaseDirectory, _fileName);

        if (File.Exists(path))
        {
            using var fs = new FileStream(path, FileMode.Open);
            var resultDeserialize = await JsonSerializer.DeserializeAsync<T>(fs);
            if (resultDeserialize == null)
                return result;

            result = resultDeserialize;
        }

        return result;
    }
}