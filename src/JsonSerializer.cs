using System.Text;
using System.Text.Json;

namespace Twitcher.PubSub;

internal static class JsonSerializer
{
    private static readonly JsonSerializerOptions _options;

    static JsonSerializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new SnakeStrategy()
        };
    }

    internal static string Serialize(object? obj) => System.Text.Json.JsonSerializer.Serialize(obj, _options);

    internal static T? Deserialize<T>(string json) => System.Text.Json.JsonSerializer.Deserialize<T>(json, _options);
}

internal class SnakeStrategy : JsonNamingPolicy
{
    public override string ConvertName(string name) => CamelToSnake(name);

    public static string CamelToSnake(string name)
    {
        var sb = new StringBuilder();
        bool isPrevious = true;
        foreach (var c in name)
        {
            if (!isPrevious & (isPrevious = (char.IsUpper(c) || char.IsDigit(c))))
                sb.Append('_');
            sb.Append(char.ToLower(c));
        }
        return sb.ToString();
    }

    public static string SnakeToCamel(string name)
    {
        var sb = new StringBuilder();
        bool isUpper = true;
        foreach (var c in name)
        {
            if (c == '_')
                isUpper = true;
            else if (isUpper)
            {
                sb.Append(char.ToUpper(c));
                isUpper = false;
            }
            else
                sb.Append(char.ToLower(c));
        }
        return sb.ToString();
    }

}
