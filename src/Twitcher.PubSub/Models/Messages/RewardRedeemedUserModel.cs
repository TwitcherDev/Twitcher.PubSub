using System.Text.Json.Serialization;

namespace Twitcher.PubSub.Models.Messages
{
    public class RewardRedeemedUserModel
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("login")]
        public string? Login { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

    }
}