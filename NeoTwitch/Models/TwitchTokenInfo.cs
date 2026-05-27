namespace NeoTwitch.Models;

public sealed class TwitchTokenInfo
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public string[] Scopes { get; set; } = [];

    public bool HasToken => !string.IsNullOrWhiteSpace(AccessToken);

    public bool NeedsRefresh => !HasToken || ExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(5);
}
