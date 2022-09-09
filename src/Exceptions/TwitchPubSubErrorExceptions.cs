namespace Twitcher.PubSub.Exceptions;

/// <summary>Twitch responded with an error message</summary>
public class TwitchPubSubErrorException : Exception
{
    /// <summary>Channel TwitchId</summary>
    public string ChannelId { get; }
    /// <summary>Topic value</summary>
    public string Topic { get; }
    /// <summary>Error from twitch</summary>
    public string Error { get; }

    internal TwitchPubSubErrorException(string channel, string topic, string error) : base($"Twitch returned '{error}' error when trying to send the topic '{topic}' for '{channel}' channel")
    {
        ChannelId = channel;
        Topic = topic;
        Error = error;
    }

    internal static TwitchPubSubErrorException Create(string channel, string topic, string error)
    {
        if (error == TwitchPubSubBadMessageErrorException.ERROR)
            return new TwitchPubSubBadMessageErrorException(channel, topic, error);
        
        if (error == TwitchPubSubBadAuthErrorException.ERROR)
            return new TwitchPubSubBadAuthErrorException(channel, topic, error);
        
        if (error == TwitchPubSubServerErrorException.ERROR)
            return new TwitchPubSubServerErrorException(channel, topic, error);

        if (error == TwitchPubSubBadTopicErrorException.ERROR)
            return new TwitchPubSubBadTopicErrorException(channel, topic, error);

        return new TwitchPubSubErrorException(channel, topic, error);
    }
}

/// <summary>Twitch responded with 'ERR_BADMESSAGE' error message</summary>
public class TwitchPubSubBadMessageErrorException : TwitchPubSubErrorException
{
    internal const string ERROR = "ERR_BADMESSAGE";

    internal TwitchPubSubBadMessageErrorException(string channel, string topic, string error) : base(channel, topic, error)
    {
    }
}

/// <summary>Twitch responded with 'ERR_BADAUTH' error message</summary>
public class TwitchPubSubBadAuthErrorException : TwitchPubSubErrorException
{
    internal const string ERROR = "ERR_BADAUTH";

    internal TwitchPubSubBadAuthErrorException(string channel, string topic, string error) : base(channel, topic, error)
    {
    }
}

/// <summary>Twitch responded with 'ERR_SERVER' error message</summary>
public class TwitchPubSubServerErrorException : TwitchPubSubErrorException
{
    internal const string ERROR = "ERR_SERVER";

    internal TwitchPubSubServerErrorException(string channel, string topic, string error) : base(channel, topic, error)
    {
    }
}

/// <summary>Twitch responded with 'ERR_BADTOPIC' error message</summary>
public class TwitchPubSubBadTopicErrorException : TwitchPubSubErrorException
{
    internal const string ERROR = "ERR_BADTOPIC";

    internal TwitchPubSubBadTopicErrorException(string channel, string topic, string error) : base(channel, topic, error)
    {
    }
}