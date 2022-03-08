using System.Text.Json.Serialization;

namespace Twitcher.PubSub
{
    public class OnMessageArgs
    {
        public string Topic { get; set; }
        public string Message { get; set; }

        public OnMessageArgs(string topic, string message)
        {
            Topic = topic;
            Message = message;
        }
    }

    public class OnMessageArgs<T>
    {
        [JsonPropertyName("data")]
        public T Data { get; set; }

        public OnMessageArgs(T data)
        {
            Data = data;
        }
    }
}