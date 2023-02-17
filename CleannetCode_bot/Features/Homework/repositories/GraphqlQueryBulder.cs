using System.Diagnostics.Contracts;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CleannetCode_bot.Features.Homework;

public class QueryBuilder
{
    private readonly string _query;
    private readonly object _variables;

    public QueryBuilder(string fileName, object variables)
    {
        var queryFilePath = Path.Combine(Environment.CurrentDirectory, "Features", "Homework", "repositories", "queries", fileName);
        _query = File.ReadAllText(queryFilePath);
        _variables = variables ?? throw new ArgumentNullException(nameof(variables));
    }

    public StringContent GetJson()
    {
        var pattern = @"\s+";
        var clearQuery = Regex.Replace(_query, pattern, " ").Trim();
        var properties = _variables.GetType().GetProperties();

        foreach (var property in properties)
        {
            var propertyValue = property.GetValue(_variables) ?? throw new ArgumentNullException(property.Name);
            var replaceValue = propertyValue is string ? $"\\\"{propertyValue}\\\"" : propertyValue.ToString();
            clearQuery = clearQuery.Replace($"${property.Name}", replaceValue);
        }

        clearQuery = clearQuery.StartsWith("query ") ? clearQuery.Substring(6) : clearQuery;

        var rawQuery = $"{{\"query\": \"{clearQuery}\" }}";

        return new StringContent(rawQuery, Encoding.UTF8, "application/json");
    }
}