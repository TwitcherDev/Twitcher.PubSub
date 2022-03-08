using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitcher.PubSub
{
    public partial class TwitcherPubSubClient
    {
        /// <summary>Add 'channel-points-channel-v1.{channelId}' topic to channel</summary>
        /// <param name="channelId">Channel TwitchId</param>
        /// <exception cref="ChannelNotFountException"></exception>
        public void AddChannelPointsTopic(string channelId) => AddTopic(channelId, "channel-points-channel-v1." + channelId);
    }
}
