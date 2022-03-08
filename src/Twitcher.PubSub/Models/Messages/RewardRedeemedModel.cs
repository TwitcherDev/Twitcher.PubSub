using System.Text.Json.Serialization;

namespace Twitcher.PubSub.Models.Messages
{
    public class RewardRedeemedModel
    {
        [JsonPropertyName("redemption")]
        public RewardRedeemedRedemptionModel? Redemption { get; set; }
    }
}
