namespace Twitcher.PubSub.Models;

internal record PubSubResponse(string Type, string? Nonce, string? Error, MessageData? Data);
internal record MessageData(string Topic, string Message);

internal record ListenRequest(string Type, string Nonce, ListenData Data);
internal record ListenData(IEnumerable<string> Topics, string AuthToken);

internal record struct DataModel<T>(T Data);
