using System.Text.Json.Serialization;

namespace Twitcher.PubSub.Models
{
    public class ListenDataModel
    {

        [JsonPropertyName("topics")]
        public IEnumerable<string>? Topics { get; set; }

        [JsonPropertyName("auth_token")]
        public string? Auth { get; set; }
    }
}