using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace CleannetCode_bot.Infrastructure.DataAccess;

public class JsonFilesGenericRepositoryOptions<TKey, TEntity>
{
    private const string KeyTemplate = @"{{Key}}";

    public JsonFilesGenericRepositoryOptions()
    {
        BasePath = Path.Combine(path1: "Data", path2: GetEntityName());
        FileFormat = $@"{GetEntityName()}.{KeyTemplate}.json";
    }

    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic), WriteIndented = true
    };

    public string BasePath { get; set; }

    // Support parameter {{Key}}
    public string FileFormat { get; set; }

    public static string GetSectionName()
    {
        return $"Json{GetEntityName()}By{GetKeyName()}";
    }

    private static string GetKeyName()
    {
        return typeof(TKey).Name;
    }

    private static string GetEntityName()
    {
        return typeof(TEntity).Name;
    }

    public string GetFilePath([DisallowNull] TKey key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        return Path.Combine(path1: BasePath, path2: FileFormat.Replace(oldValue: KeyTemplate, newValue: key.ToString()));
    }
}