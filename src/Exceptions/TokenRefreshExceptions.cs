namespace Twitcher.PubSub.Exceptions;

/// <summary>The refresh token handler returned <see langword="null"/></summary>
public class TokenNotRefreshedException : Exception
{
    internal TokenNotRefreshedException() : base("The refresh token handler returned null") { }
}

/// <summary>The refresh token handler threw an exception</summary>
public class TokenRefreshException : Exception
{
    internal TokenRefreshException(Exception? exception) : base("The refresh token handler threw an exception", exception) { }
}
