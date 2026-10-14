namespace NeoTwitch.Services;

public static class TwitchConnectionRecoveryService
{
    public static bool IsRecoverableRefreshError(Exception exception)
    {
        var normalizedMessage = new string(exception.Message
            .Where(char.IsLetterOrDigit)
            .ToArray());

        return normalizedMessage.Contains("missingclientsecret", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("invalidclient", StringComparison.OrdinalIgnoreCase)
            || normalizedMessage.Contains("invalidrefreshtoken", StringComparison.OrdinalIgnoreCase);
    }
}
