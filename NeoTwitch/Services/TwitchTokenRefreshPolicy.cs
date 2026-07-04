using NeoTwitch.Models;

namespace NeoTwitch.Services;

public static class TwitchTokenRefreshPolicy
{
    private static readonly TimeSpan DefaultRefreshWindow = TimeSpan.FromMinutes(5);

    public static bool NeedsRefresh(TwitchTokenInfo token, DateTimeOffset now)
    {
        return NeedsRefresh(token, now, DefaultRefreshWindow);
    }

    public static bool NeedsRefresh(TwitchTokenInfo token, DateTimeOffset now, TimeSpan refreshWindow)
    {
        return !token.HasToken || token.ExpiresAt <= now.Add(refreshWindow);
    }
}
