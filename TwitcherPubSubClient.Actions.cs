using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Twitcher.PubSub.Models;
using Twitcher.PubSub.Models.Messages;

namespace Twitcher.PubSub
{
    public partial class TwitcherPubSubClient
    {
        public event EventHandler<OnMessageArgs<RewardRedeemedModel>?>? OnRewardRedeemed;

        private void Actions(MessageModel message)
        {
            if (message.Data == default || message.Data.Topic == default || message.Data.Message == default)
                return;
            if (message.Data.Topic.StartsWith("channel-points-channel-v1."))
            {
                OnRewardRedeemed?.Invoke(this, JsonSerializer.Deserialize<OnMessageArgs<RewardRedeemedModel>>(message.Data.Message));
            }
        }
    }
}
