namespace Twitcher.PubSub;

/// <summary></summary>
public interface IPubSubChannel
{
    /// <summary>Channel TwitchId</summary>
    string ChannelId { get; }
    /// <summary>Channel auth (access) token</summary>
    string Auth { get; }
}

/// <summary></summary>
public interface IPubSubTopic
{
    /// <summary>Channel TwitchId</summary>
    string ChannelId { get; }
    /// <summary>Topic value</summary>
    string TopicLine { get; }
}

internal class Channel : IPubSubChannel
{
    public string ChannelId { get; }
    public string Auth { get; set; }

    internal Channel(string channelId, string auth)
    {
        ChannelId = channelId;
        Auth = auth;
    }
}

internal class TopicRequest
{
    internal string ChannelId { get; }
    internal string Auth { get; set; }
    internal string TopicLine { get; }

    internal string Nonce { get; }
    internal bool IsListen { get; }
    internal TaskCompletionSource? Task { get; }
    internal CancellationToken Token { get; }

    internal string Type => IsListen ? "LISTEN" : "UNLISTEN";

    public TopicRequest(string channelId, string auth, string topicLine, string nonce, bool isListen, TaskCompletionSource? task, CancellationToken token)
    {
        ChannelId = channelId;
        Auth = auth;
        TopicLine = topicLine;
        Nonce = nonce;
        IsListen = isListen;
        Task = task;
        Token = token;
    }
}

internal class Topic : IPubSubTopic
{
    public string ChannelId { get; }
    public string TopicLine { get; }

    public Topic(string channelId, string topicLine)
    {
        ChannelId = channelId;
        TopicLine = topicLine;
    }
}
