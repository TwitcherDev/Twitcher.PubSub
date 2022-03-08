using System.Text.Json.Serialization;

namespace Twitcher.PubSub.Models
{
    public class MessageDataModel
    {
        [JsonPropertyName("topic")]
        public string? Topic { get; set; }
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}