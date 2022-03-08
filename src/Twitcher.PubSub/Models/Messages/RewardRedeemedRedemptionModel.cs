using System.Text.Json.Serialization;

namespace Twitcher.PubSub.Models.Messages
{
    public class RewardRedeemedRedemptionModel
    {
        [JsonPropertyName("channel_id")]
        public string? ChannelId { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("user")]
        public RewardRedeemedUserModel? User { get; set; }

        [JsonPropertyName("reward")]
        public RewardRedeemedRewardModel? Reward { get; set; }

    }
}