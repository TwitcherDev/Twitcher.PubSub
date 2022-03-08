using Twitcher.PubSub.Enums;

namespace Twitcher.PubSub
{
    public class PubSubChannel
    {
        public string ChannelId { get; set; }
        public string Auth { get; set; }
        public bool IsChanged { get; set; }
        public List<string> Topics { get; private set; }
        public string? Nonce { get; set; }

        public bool IsClear { get; set; }
        public List<string> ListenTopics { get; private set; }
        public PubSubChannelResponceType ResponceType { get; set; }

        public PubSubChannel(string channelId, string auth)
        {
            ChannelId = channelId;
            Auth = auth;
            Topics = new List<string>();
            ListenTopics = new List<string>();
            ResponceType = PubSubChannelResponceType.None;
        }
    }
}