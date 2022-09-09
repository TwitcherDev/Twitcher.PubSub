using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using Twitcher.PubSub.Exceptions;
using Twitcher.PubSub.Models;

namespace Twitcher.PubSub;

/// <summary>Client for communicating with Twitch API pubsub</summary>
/// <remarks>Based on twitch official docs <see href="https://dev.twitch.tv/docs/pubsub"/></remarks>
public partial class TwitcherPubSubClient : IDisposable
{
    private const string SERVER = "wss://pubsub-edge.twitch.tv";

    private readonly ILogger? _logger;

    private readonly List<Channel> _channels;
    private readonly List<TopicRequest> _requests;
    private readonly List<Topic> _topics;

    private ClientWebSocket? _socket;

    private bool _isShouldBeConnected;
    private bool _isConnected;
    private int _reconnectInterval;
    private Task? _reconnectingTask;
    private bool _isPong;

    private CancellationTokenSource? _tasksToken;
    private Task? _pingingTask;
    private Task? _receivingTask;

    private RefreshFunc? _tokenRefreshFunc;

    /// <summary>Whether the client is currently connected to the server</summary>
    public bool IsConnected => _isConnected;

    /// <summary>Get a list of all channels registered in the client</summary>
    public IReadOnlyCollection<IPubSubChannel> Channels
    {
        get
        {
            lock (_channels)
                return _channels.AsReadOnly();
        }
    }

    /// <summary>Get a list of all topics registered in the client</summary>
    public IReadOnlyCollection<IPubSubTopic> Topics
    {
        get
        {
            lock (_topics)
                return _topics.AsReadOnly();
        }
    }

    /// <summary>Invoked when the client connects to the server</summary>
    public event EventHandler? OnConnected;
    /// <summary>Invoked when the client disconnects from the server</summary>
    public event EventHandler? OnDisconnected;
    /// <summary>Invoked when the client tries to connect to the server</summary>
    public event EventHandler? OnReconnecting;

    /// <summary>Invoked when the client receives message</summary>
    public event EventHandler<MessageEventArgs>? OnMessage;

    /// <summary>Creates <see cref="TwitcherPubSubClient"/> instance</summary>
    /// <param name="logger"><see cref="ILogger"/> for logging</param>
    public TwitcherPubSubClient(ILogger? logger = null)
    {
        _logger = logger;

        _isShouldBeConnected = false;
        _isConnected = false;
        _reconnectInterval = 1000;
        _isPong = false;

        _channels = new List<Channel>();
        _requests = new List<TopicRequest>();
        _topics = new List<Topic>();
    }

    /// <summary>Delegate for <see cref="TokenRefreshHandler"/></summary>
    /// <param name="channelId">Channel TwitchId</param>
    /// <param name="oldAuth">Old auth (access) token</param>
    /// <returns><see cref="Task"/> that should return a new token or <see langword="null"/> if it failed to get a new token</returns>
    public delegate Task<string?> RefreshFunc(string channelId, string oldAuth);

    /// <summary>If set, it will be invoked on a bad auth error to automatically get a new token and make a second request</summary>
    public RefreshFunc? TokenRefreshHandler { set => _tokenRefreshFunc = value; }

    /// <summary>Connect automatically when first adding a topic</summary>
    public void DelayedConnect()
    {
        _isShouldBeConnected = true;
    }

    /// <summary>Connects to the server</summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns><see cref="Task"/> to track the result</returns>
    public Task Connect(CancellationToken cancellationToken = default)
    {
        _isShouldBeConnected = true;
        return ConnectCore(cancellationToken);
    }

    /// <summary>Disconnects from the server</summary>
    /// <returns><see cref="Task"/> to track the result</returns>
    public async Task Disconnect()
    {
        _isShouldBeConnected = false;
        
        _tasksToken?.Cancel();
        _tasksToken = null;
        if (_pingingTask != null)
            try { await _pingingTask; } catch (OperationCanceledException) { }
        if (_receivingTask != null)
            try { await _receivingTask; } catch (OperationCanceledException) { }

        if (_reconnectingTask != null)
            try { await _reconnectingTask; } catch (OperationCanceledException) { return; }
        Disconnecting();
    }

    private Task ConnectCore(CancellationToken cancellationToken = default)
    {
        if (_isShouldBeConnected)
            return _reconnectingTask ??= Reconnecting(cancellationToken);
        throw new OperationCanceledException();
    }

    private async Task Reconnecting(CancellationToken cancellationToken)
    {
        if (_socket != null)
        {
            Disconnecting();
            _logger?.LogInformation("Reconnecting");
            OnReconnecting?.Invoke(this, EventArgs.Empty);
        }

        while (!cancellationToken.IsCancellationRequested && _isShouldBeConnected)
        {
            _socket = new ClientWebSocket();
            try
            {
                await _socket.ConnectAsync(new Uri(SERVER), cancellationToken);
            }
            catch (WebSocketException e)
            {
                _logger?.LogError(e, "Connection error");
            }
            if (_socket.State != WebSocketState.Open)
            {
                Disconnecting();
                var reconnectIn = _reconnectInterval + Random.Shared.Next(500);
                _logger?.LogDebug("Try to reconnect in {time} seconds", reconnectIn / 1000d);
                await Task.Delay(reconnectIn, cancellationToken);
                _reconnectInterval = Math.Min(_reconnectInterval * 2, 120000);
                _logger?.LogInformation("Reconnecting");
                OnReconnecting?.Invoke(this, EventArgs.Empty);
                continue;
            }
            _isConnected = true;
            _logger?.LogInformation("Connected");
            OnConnected?.Invoke(this, EventArgs.Empty);
            _reconnectInterval = 1000;

            if (_tasksToken == null)
            {
                _tasksToken = new CancellationTokenSource();
                _pingingTask = Pinging(_tasksToken.Token);
                _receivingTask = Receiving(_tasksToken.Token);
            }

            _ = Task.Run(() => SendAllTopics(cancellationToken), cancellationToken);

            _reconnectingTask = null;
            return;
        }
        _reconnectingTask = null;
        if (_isShouldBeConnected)
            throw new OperationCanceledException(cancellationToken);
    }

    private void Disconnecting()
    {
        if (_socket == null)
            return;

        _socket.Dispose();
        _socket = null;
        _isConnected = false;
        _logger?.LogInformation("Disconnected");
        OnDisconnected?.Invoke(this, EventArgs.Empty);
    }
    
    private async void SendAllTopics(CancellationToken cancellationToken)
    {
        List<(string channel, string line)> topics;
        lock (_topics)
        lock (_requests)
        {
            topics = _topics
                .Where(t => !_requests.Any(r => r.ChannelId == t.ChannelId && r.TopicLine == t.TopicLine && !r.IsListen))
                .Select(t => (channel: t.ChannelId, line: t.TopicLine))
                .Concat(_requests.Where(r => r.IsListen).Select(r => (channel: r.ChannelId, line: r.TopicLine))).ToList();
            _topics.Clear();
            foreach (var request in _requests)
                request.Task?.TrySetCanceled(CancellationToken.None);
            _requests.Clear();
        }
        try
        {
            foreach (var topic in topics)
            {
                var channel = _channels.FirstOrDefault(c => c.ChannelId == topic.channel);
                if (channel == null)
                    continue;
                var request = new TopicRequest(topic.channel, channel.Auth, topic.line, GenerateNonce(), true, null, cancellationToken);
                lock (_requests)
                    _requests.Add(request);
                await SendTopic(request);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task Pinging(CancellationToken cancellationToken)
    {
        var time = DateTime.UtcNow;
        while (!cancellationToken.IsCancellationRequested && _isConnected)
        {
            var t = (int)((time += TimeSpan.FromMinutes(4 - Random.Shared.NextDouble() / 2)) - DateTime.Now).TotalMilliseconds;
            if (t > 0)
                await Task.Delay(t, cancellationToken);

            _isPong = false;
            await Send(@"{""type"":""PING""}", cancellationToken);
            await Task.Delay(12000, cancellationToken);
            if (!_isPong)
                await ConnectCore(cancellationToken);
        }
    }

    private async Task Receiving(CancellationToken cancellationToken)
    {
        var rcvBytes = new byte[8192];
        var rcvBuffer = new ArraySegment<byte>(rcvBytes);
        var isEmpty = true;
        var isReconnectRequired = false;
        var messageBuffer = new StringBuilder();
        while (!cancellationToken.IsCancellationRequested && _isConnected)
        {
            if (_socket == null || isReconnectRequired)
            {
                isReconnectRequired = false;
                await ConnectCore(cancellationToken);
                continue;
            }
            WebSocketReceiveResult rcvResult;
            try
            {
                rcvResult = await _socket.ReceiveAsync(rcvBuffer, cancellationToken);
            }
            catch (WebSocketException)
            {
                await ConnectCore(cancellationToken);
                continue;
            }
            byte[] msgBytes = rcvBuffer.Skip(rcvBuffer.Offset).Take(rcvResult.Count).ToArray();
            string rcvMsg = Encoding.UTF8.GetString(msgBytes);
            if (isEmpty && rcvResult.EndOfMessage)
            {
                isReconnectRequired = MessageReceived(rcvMsg);
                continue;
            }
            isEmpty = false;
            messageBuffer.Append(rcvMsg);
            if (rcvResult.EndOfMessage)
            {
                isReconnectRequired = MessageReceived(messageBuffer.ToString());
                isEmpty = true;
                messageBuffer.Clear();
            }
        }
    }

    private Task Send(object obj, CancellationToken cancellationToken) => Send(JsonSerializer.Serialize(obj), cancellationToken);
    private Task Send(string message, CancellationToken cancellationToken)
    {
        if (_socket == null)
            throw new InvalidOperationException();
        _logger?.LogTrace("Send message: {message}", message);
        byte[] sendBytes = Encoding.UTF8.GetBytes(message);
        var sendBuffer = new ArraySegment<byte>(sendBytes);
        return _socket.SendAsync(sendBuffer, WebSocketMessageType.Text, endOfMessage: true, cancellationToken: cancellationToken);
    }

    private async Task SendTopic(TopicRequest request)
    {
        await Send(new ListenRequest(request.Type, request.Nonce, new ListenData(new[] { request.TopicLine }, request.Auth)), request.Token);
        _logger?.LogDebug("{request} topic '{topic}' on '{channel}' sent", request.Type, request.TopicLine, request.ChannelId);
    }

    private bool MessageReceived(string text)
    {
        _logger?.LogTrace("Message received: {message}", text);
        var response = JsonSerializer.Deserialize<PubSubResponse>(text)!;
        switch (response.Type)
        {
            case "PONG":
                _isPong = true;
                break;

            case "RECONNECT":
                return true;

            case "RESPONSE":
                Debug.Assert(response.Nonce != null);
                Debug.Assert(response.Error != null);

                ResponseTopic(response.Nonce, response.Error);
                break;

            case "MESSAGE":
                Debug.Assert(response.Data != null);
                Debug.Assert(response.Data.Topic != null);
                Debug.Assert(response.Data.Message != null);

                if (response?.Data?.Topic == null || response.Data.Message == null)
                    break;
                OnMessage?.Invoke(this, new MessageEventArgs(response.Data.Topic, response.Data.Message));
                Actions(response.Data);
                break;

            default:
                _logger?.LogWarning("Unknown message '{message}'", text);
                break;
        }
        return false;
    }

    /// <summary>Adds new channel or update auth if channel exist</summary>
    /// <param name="channelId">Channel TwitchId</param>
    /// <param name="auth">Auth (access) token</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void AddChannel(string channelId, string auth)
    {
        ArgumentNullException.ThrowIfNull(channelId);
        ArgumentNullException.ThrowIfNull(auth);

        var channel = _channels.FirstOrDefault(c => c.ChannelId == channelId);
        if (channel != null)
        {
            channel.Auth = auth;
            _logger?.LogDebug("Channel '{channel}' auth updated", channelId);
        }
        else
        {
            _channels.Add(new Channel(channelId, auth));
            _logger?.LogDebug("Channel '{channel}' added", channelId);
        }
    }

    /// <summary>Removes the channel. Does not cancel the listening on topics on the channel</summary>
    /// <param name="channelId">Channel TwitchId</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <returns><see langword="true"/> if channel is successfully removed; otherwise, <see langword="false"/></returns>
    public bool RemoveChannel(string channelId)
    {
        ArgumentNullException.ThrowIfNull(channelId);

        var channel = _channels.FirstOrDefault(c => c.ChannelId == channelId);
        if (channel == null || !_channels.Remove(channel))
            return false;
        _logger?.LogDebug("Channel '{channel}' removed", channelId);
        return true;
    }

    /// <summary>Adds any custom topic to the channel to listen to</summary>
    /// <param name="channelId">Channel TwitchId</param>
    /// <param name="topic">Topic line</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns><see cref="Task"/> to track the result</returns>
    /// <exception cref="TwitchPubSubErrorException"></exception>
    public Task AddTopic(string channelId, string topic, CancellationToken cancellationToken = default) => EditTopic(channelId, true, topic, cancellationToken);

    /// <summary>Removes any custom topic to the channel to listen to</summary>
    /// <param name="channelId">Channel TwitchId</param>
    /// <param name="topic">Topic line</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns><see cref="Task"/> to track the result</returns>
    /// <exception cref="TwitchPubSubErrorException"></exception>
    public Task RemoveTopic(string channelId, string topic, CancellationToken cancellationToken = default) => EditTopic(channelId, false, topic, cancellationToken);

    private async Task EditTopic(string channelId, bool isListen, string topicLine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(topicLine);

        if (!_isShouldBeConnected)
            throw new InvalidOperationException($"Call {nameof(Connect)} or {nameof(DelayedConnect)} before editing topics");

        if (!_isConnected)
            await ConnectCore(cancellationToken);

        var channel = _channels.FirstOrDefault(c => c.ChannelId == channelId);
        if (channel == null)
            throw new KeyNotFoundException($"Channel not fount. Call {nameof(AddChannel)} before editing channel topics");

        var request = new TopicRequest(channelId, channel.Auth, topicLine, GenerateNonce(), isListen, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously), cancellationToken);
        lock (_requests)
            _requests.Add(request);
        await SendTopic(request);
        if (request.Task != null)
            await request.Task.Task;
    }

    private async void ResponseTopic(string? nonce, string? error)
    {
        TopicRequest? request;
        lock (_requests)
        {
            request = _requests.FirstOrDefault(r => r.Nonce == nonce);
            if (request == null)
                return;
            _requests.Remove(request);
        }

        if (string.IsNullOrEmpty(error))
        {
            lock (_topics)
            {
                if (request.IsListen && !_topics.Any(t => t.ChannelId == request.ChannelId && t.TopicLine == request.TopicLine))
                    _topics.Add(new Topic(request.ChannelId, request.TopicLine));
                else if (!request.IsListen)
                    _topics.RemoveAll(t => t.ChannelId == request.ChannelId && t.TopicLine == request.TopicLine);
            }
            _logger?.LogDebug("Response to {request} topic '{topic}' on '{channel}' processed", request.Type, request.TopicLine, request.ChannelId);
            request.Task?.TrySetResult();
            return;
        }
        if (error == "ERR_BADAUTH" && _tokenRefreshFunc != null)
        {
            var channel = _channels.FirstOrDefault(c => c.ChannelId == request.ChannelId);
            if (channel != null)
            {
                if (channel.Auth != request.Auth)
                {
                    string? auth;
                    try
                    {
                        auth = await _tokenRefreshFunc!(channel.ChannelId, channel.Auth);
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e, "Channel '{channel}' auth refresh threw an exception", request.ChannelId);
                        request.Task?.TrySetException(new TokenRefreshException(e));
                        return;
                    }
                    if (string.IsNullOrEmpty(auth))
                    {
                        _logger?.LogDebug("Channel '{channel}' auth not refreshed", request.ChannelId);
                        request.Task?.TrySetException(new TokenNotRefreshedException());
                        return;
                    }
                    channel.Auth = auth;
                    _logger?.LogDebug("Channel '{channel}' auth refreshed", request.ChannelId);
                }
                request.Auth = channel.Auth;
                if (request.Token.IsCancellationRequested)
                {
                    request.Task?.TrySetException(new OperationCanceledException(request.Token));
                    return;
                }
                lock (_requests)
                    _requests.Add(request);
                await SendTopic(request);
                return;
            }
        }
        _logger?.LogDebug("{request} topic '{topic}' on '{channel}' failed with error '{error}'", request.Type, request.TopicLine, request.ChannelId, error);
        request.Task?.TrySetException(TwitchPubSubErrorException.Create(request.ChannelId, request.TopicLine, error));
    }

    private const int NONCE_LENGTH = 8;
    private const string NONCE_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private static string GenerateNonce() => new(Enumerable.Range(0, NONCE_LENGTH).Select(_ => NONCE_CHARS[Random.Shared.Next(NONCE_CHARS.Length)]).ToArray());

    private bool _disposedValue;
    /// <inheritdoc/>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            _isShouldBeConnected = false;
            _tasksToken?.Cancel();
            if (disposing)
            {
                _socket?.Dispose();
                _reconnectingTask?.Dispose();
                _pingingTask?.Dispose();
                _receivingTask?.Dispose();
                _tasksToken?.Dispose();
            }
            foreach (var request in _requests)
            {
                request.Task?.TrySetCanceled();
                if (disposing)
                    request.Task?.Task?.Dispose();
            }
            _disposedValue = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
