using System.Text.Json.Serialization;

namespace Twitcher.PubSub.Models.Messages
{
    public class RewardRedeemedRewardModel
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("cost")]
        public int Cost { get; set; }
    }
}