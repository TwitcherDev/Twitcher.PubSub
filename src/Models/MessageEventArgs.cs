namespace Twitcher.PubSub.Models;

public class MessageEventArgs : EventArgs
{
    public string Topic { get; set; }
    public string Message { get; set; }

    public MessageEventArgs(string topic, string message)
    {
        Topic = topic;
        Message = message;
    }
}

public class MessageEventArgs<T> : EventArgs
{
    public string Topic { get; set; }
    public T Data { get; set; }

    public MessageEventArgs(string topic, T data)
    {
        Topic = topic;
        Data = data;
    }
}