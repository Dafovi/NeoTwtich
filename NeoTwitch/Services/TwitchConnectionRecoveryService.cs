namespace NeoTwitch.Services;

public static class TwitchConnectionRecoveryService
{
    public static bool IsRecoverableRefreshError(Exception exception)
    {
        var message = exception.Message;
        return message.Contains("missing client secret", StringComparison.OrdinalIgnoreCase)
            || message.Contains("invalid client", StringComparison.OrdinalIgnoreCase)
            || message.Contains("invalid refresh token", StringComparison.OrdinalIgnoreCase);
    }
}
