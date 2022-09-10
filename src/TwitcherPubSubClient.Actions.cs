using Twitcher.PubSub.Models;

namespace Twitcher.PubSub;

public partial class TwitcherPubSubClient
{
    /// <summary>A custom reward is redeemed in a channel</summary>
    public event EventHandler<MessageEventArgs<RewardRedeemedModel>>? OnRewardRedeemed;

    private void Actions(MessageData message)
    {
        if (message.Topic.StartsWith("channel-points-channel-v1.") && OnRewardRedeemed != null)
        {
            var data = JsonSerializer.Deserialize<DataModel<RewardRedeemedModel>?>(message.Message)?.Data;
            if (data != null)
                OnRewardRedeemed.Invoke(this, new MessageEventArgs<RewardRedeemedModel>(message.Topic, data));
        }
    }
}
