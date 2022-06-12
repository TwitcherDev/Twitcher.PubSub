using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Twitcher.PubSub.Enums;
using Twitcher.PubSub.Exceptions;
using Twitcher.PubSub.Models;
using Twitcher.PubSub.Models.Events;

namespace Twitcher.PubSub
{
    public partial class TwitcherPubSubClient : IDisposable
    {
        private readonly CancellationToken _token;
        private ClientWebSocket? _socket;
        private bool _isConnected;
        private bool _isReconnecting;

        private bool _isPong;
        private int _reconnectInterval;
        
        private readonly List<PubSubChannel> _channels;

        public event EventHandler<OnPubSubErrorArgs>? OnError;
        public event EventHandler<EventArgs>? OnConnected;
        public event EventHandler<EventArgs>? OnReconnected;
        public event EventHandler<OnMessageArgs>? OnMessage;

        private Func<string, string, Task<string?>>? _tokenRefreshFunc;

        public TwitcherPubSubClient(CancellationToken token = default)
        {
            _token = token;
            _isConnected = false;

            _isPong = false;
            _reconnectInterval = 1000;
            
            _channels = new List<PubSubChannel>();
        }

        public Func<string, string, Task<string?>>? TokenRefreshHandler { set => _tokenRefreshFunc = value; }

        /// <summary>Dont use it before add channel and topic</summary>
        public void Connect()
        {
            if (_socket != null)
                return;

            _ = Reconnecting();

            _ = Pinging();
        }

        public void Reconnect()
        {
            if (_socket == null || _isReconnecting)
                return;
            _ = Reconnecting();
        }

        public void Disconnect()
        {
            if (_socket == null)
                return;
            _ = Disconnecting();
        }

        private async Task Reconnecting()
        {
            if (_socket == null || _isReconnecting)
                return;

            _isReconnecting = true;
            foreach (var channel in _channels)
            {
                channel.ResponceType = PubSubChannelResponceType.None;
                channel.ListenTopics.Clear();
                channel.IsChanged = true;
            }

            while (!_token.IsCancellationRequested)
            {
                _socket = new ClientWebSocket();
                try
                {
                    await _socket.ConnectAsync(new Uri("wss://pubsub-edge.twitch.tv"), _token);
                }
                catch (WebSocketException) { }
                if (_socket.State == WebSocketState.Open)
                {
                    if (_isConnected)
                        OnReconnected?.Invoke(this, EventArgs.Empty);
                    else
                    {
                        _isConnected = true;
                        OnConnected?.Invoke(this, EventArgs.Empty);
                    }
                    _reconnectInterval = 1000;
                    _ = Receiving();
                    UpdateNext();
                    _isReconnecting = false;
                    return;
                }
                await Disconnecting();
                await Task.Delay(_reconnectInterval + Random.Shared.Next(500), _token);
                _reconnectInterval = Math.Min(_reconnectInterval * 2, 120000);
            }
        }

        private async Task Disconnecting()
        {
            if (_socket == null)
                return;
            if (_socket.State == WebSocketState.Open)
            {
                await _socket.CloseAsync(WebSocketCloseStatus.Empty, null, _token);
            }
            _socket.Dispose();
            _socket = null;
        }
        
        private async Task Pinging()
        {
            try
            {
                var time = DateTime.Now;
                while (!_token.IsCancellationRequested)
                {
                    var t = (int)((time += TimeSpan.FromMinutes(4 - Random.Shared.NextDouble() / 2)) - DateTime.Now).TotalMilliseconds;
                    if (t > 0)
                        await Task.Delay(t, _token);

                    if (_socket == null || _socket.State != WebSocketState.Open)
                        continue;

                    _isPong = false;
                    await Send(@"{""type"":""PING""}");
                    await Task.Delay(12000, _token);
                    if (!_isPong)
                        _ = Reconnecting();
                }
            }
            catch (TaskCanceledException) { }
        }

        private async Task Receiving()
        {
            try
            {
                var rcvBytes = new byte[8192];
                var rcvBuffer = new ArraySegment<byte>(rcvBytes);
                var messageBuffer = string.Empty;
                while (!_token.IsCancellationRequested)
                {
                    if (_socket == null || _socket.State != WebSocketState.Open)
                        return;
                    WebSocketReceiveResult rcvResult;
                    try
                    {
                        rcvResult = await _socket.ReceiveAsync(rcvBuffer, _token);
                    }
                    catch (WebSocketException)
                    {
                        _ = Reconnecting();
                        return;
                    }
                    byte[] msgBytes = rcvBuffer.Skip(rcvBuffer.Offset).Take(rcvResult.Count).ToArray();
                    string rcvMsg = Encoding.UTF8.GetString(msgBytes);
                    messageBuffer += rcvMsg;
                    if (rcvResult.EndOfMessage)
                    {
                        _ = MessageReceived(messageBuffer);
                        messageBuffer = string.Empty;
                    }
                }
            }
            catch (TaskCanceledException) { }
        }

        private Task Send(object obj) => Send(JsonSerializer.Serialize(obj));
        private async Task Send(string message)
        {
            if (_socket == null)
                return;
            byte[] sendBytes = Encoding.UTF8.GetBytes(message);
            var sendBuffer = new ArraySegment<byte>(sendBytes);
            try
            {
                await _socket.SendAsync(sendBuffer, WebSocketMessageType.Text, endOfMessage: true, cancellationToken: _token);
            }
            catch (WebSocketException)
            {
                _ = Reconnecting();
            }
        }

        private async Task MessageReceived(string text)
        {
            var typeText = text.Replace(" ", "");
            if (typeText.StartsWith(@"{""type"":""PONG"""))
            {
                _isPong = true;
                return;
            }
            if (typeText.StartsWith(@"{""type"":""RECONNECT"""))
            {
                _ = Reconnecting();
                return;
            }
            if (typeText.StartsWith(@"{""type"":""RESPONSE"""))
            {
                ResponceModel? responce;
                try
                {
                    responce = JsonSerializer.Deserialize<ResponceModel>(text);
                }
                catch (JsonException e)
                {
                    OnError?.Invoke(this, new OnPubSubErrorArgs { Type = PubSubErrorType.JsonException, Exception = e });
                    return;
                }
                if (responce == default)
                {
                    OnError?.Invoke(this, new OnPubSubErrorArgs { Type = PubSubErrorType.UnknownType, Message = text });
                    return;
                }
                switch (responce.Error)
                {
                    case "":
                        {
                            var channel = _channels.FirstOrDefault(c => c.Nonce == responce.Nonce);
                            if (channel?.ResponceType == PubSubChannelResponceType.Listen)
                            {
                                channel.ListenTopics.Clear();
                                channel.ListenTopics.AddRange(channel.Topics);
                            }
                            else if (channel?.ResponceType == PubSubChannelResponceType.Unlisten)
                                channel.IsClear = false;
                            UpdateNext();
                        }
                        return;

                    case "ERR_BADMESSAGE":
                        OnError?.Invoke(this, new OnPubSubErrorArgs { Type = PubSubErrorType.BadMessage });
                        return;

                    case "ERR_BADAUTH":
                        if (_tokenRefreshFunc != default)
                        {
                            var channel = _channels.FirstOrDefault(c => c.Nonce == responce.Nonce);
                            if (channel != default)
                            {
                                string? auth;
                                try
                                {
                                    auth = await _tokenRefreshFunc(channel.ChannelId, channel.Auth);
                                }
                                catch (Exception e)
                                {
                                    OnError?.Invoke(this, new OnPubSubErrorArgs { Type = PubSubErrorType.RefreshException, Exception = e });
                                    return;
                                }
                                if (!string.IsNullOrEmpty(auth))
                                {
                                    channel.Auth = auth.Split(':').First();
                                    channel.IsChanged = true;
                                    UpdateNext();
                                }
                                else
                                    _channels.Remove(channel);
                            }
                        }
                        else
                            OnError?.Invoke(this, new OnPubSubErrorArgs { Type = PubSubErrorType.BadAuth });
                        return;

                    case "ERR_SERVER":
                        OnError?.Invoke(this, new OnPubSubErrorArgs { Type = PubSubErrorType.Server });
                        return;

                    case "ERR_BADTOPIC":
                        OnError?.Invoke(this, new OnPubSubErrorArgs { Type = PubSubErrorType.BadTopic });
                        return;

                    default:
                        OnError?.Invoke(this, new OnPubSubErrorArgs { Type = PubSubErrorType.UnknownErrorType, Message = responce.Error });
                        return;
                }
            }
            if (typeText.StartsWith(@"{""type"":""MESSAGE"""))
            {
                MessageModel? message;
                try
                {
                    message = JsonSerializer.Deserialize<MessageModel>(text);
                }
                catch (JsonException e)
                {
                    OnError?.Invoke(this, new OnPubSubErrorArgs { Type = PubSubErrorType.JsonException, Exception = e });
                    return;
                }
                if (message?.Data?.Topic == default || message?.Data?.Message == default)
                {
                    OnError?.Invoke(this, new OnPubSubErrorArgs { Type = PubSubErrorType.UnknownType, Message = text });
                    return;
                }
                OnMessage?.Invoke(this, new OnMessageArgs(message.Data.Topic, message.Data.Message));
                Actions(message);
            }
            else
                OnError?.Invoke(this, new OnPubSubErrorArgs { Type = PubSubErrorType.UnknownType, Message = text});
        }

        public void UpdateListens()
        {
            UpdateNext();
        }

        private void UpdateNext()
        {
            var channel = _channels.FirstOrDefault(c => c.IsChanged);
            if (channel == null)
                return;
            if (string.IsNullOrEmpty(channel.Nonce))
                do
                    channel.Nonce = Nonce(8);
                while (!_channels.Any(c => c.Nonce == channel.Nonce));

            if (channel.IsClear)
            {
                channel.ResponceType = PubSubChannelResponceType.Unlisten;
                _ = Send(new ListenModel { Type = "UNLISTEN", Nonce = channel.Nonce, Data = new ListenDataModel { Topics = channel.ListenTopics, Auth = channel.Auth } });
                if (!channel.Topics.Any())
                    channel.IsChanged = false;
                return;
            }

            if (channel.Topics.Any())
            {
                channel.ResponceType = PubSubChannelResponceType.Listen;
                _ = Send(new ListenModel { Type = "LISTEN", Nonce = channel.Nonce, Data = new ListenDataModel { Topics = channel.Topics, Auth = channel.Auth } });
                channel.IsChanged = false;
            }
        }

        /// <summary>Add new channel or update auth if exist</summary>
        /// <param name="channelId">Channel TwitchId</param>
        /// <param name="auth">Auth token</param>
        public void AddChannel(string channelId, string auth)
        {
            if (string.IsNullOrWhiteSpace(auth))
                throw new ArgumentNullException(nameof(auth));
            var access = auth.Split(':').First();
            var channel = _channels.FirstOrDefault(c => c.ChannelId == channelId);
            if (channel != default)
                channel.Auth = access;
            else
                _channels.Add(new PubSubChannel(channelId, access));
        }

        /// <summary>Add any topic to channel</summary>
        /// <param name="channelId">Channel TwitchId</param>
        /// <param name="topic">Topic string</param>
        /// <exception cref="ChannelNotFountException"></exception>
        public void AddTopic(string channelId, string topic)
        {
            var channel = _channels.FirstOrDefault(c => c.ChannelId == channelId);
            if (channel == default)
                throw new ChannelNotFountException(channelId);
            channel.IsChanged = true;
            channel.Topics.Add(topic);
        }

        /// <summary>Remove all topics from channel</summary>
        /// <param name="channelId">Channel TwitchId</param>
        /// <exception cref="ChannelNotFountException"></exception>
        public void ClearTopics(string channelId)
        {
            var channel = _channels.FirstOrDefault(c => c.ChannelId == channelId);
            if (channel == default)
                throw new ChannelNotFountException(channelId);
            channel.IsChanged = true;
            channel.IsClear = true;
            channel.Topics.Clear();
        }

        private static string Nonce(int length) => new(Enumerable.Repeat("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", length).Select(s => s[Random.Shared.Next(s.Length)]).ToArray());

        public void Dispose()
        {
            Disconnecting().Wait();
            GC.SuppressFinalize(this);
        }
    }
}
