using Twitcher.PubSub.Exceptions;

namespace Twitcher.PubSub;

public partial class TwitcherPubSubClient
{
    /// <summary>Add 'channel-points-channel-v1.{channelId}' topic to channel</summary>
    /// <param name="channelId">Channel TwitchId</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns><see cref="Task"/> to track the result</returns>
    /// <exception cref="TwitchPubSubErrorException"></exception>
    public Task AddChannelPointsTopic(string channelId, CancellationToken cancellationToken = default) => AddTopic(channelId, "channel-points-channel-v1." + channelId, cancellationToken);

    /// <summary>Remove 'channel-points-channel-v1.{channelId}' topic from channel</summary>
    /// <param name="channelId">Channel TwitchId</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns><see cref="Task"/> to track the result</returns>
    /// <exception cref="TwitchPubSubErrorException"></exception>
    public Task RemoveChannelPointsTopic(string channelId, CancellationToken cancellationToken = default) => RemoveTopic(channelId, "channel-points-channel-v1." + channelId, cancellationToken);
}
