namespace NeoTwitch.Services;

public static class TwitchAuthProtocol
{
    public const string DeviceCodeUrl = "https://id.twitch.tv/oauth2/device";
    public const string TokenUrl = "https://id.twitch.tv/oauth2/token";
    public const string UsersUrl = "https://api.twitch.tv/helix/users";
    public const string StreamsUrl = "https://api.twitch.tv/helix/streams";
    public const string BearerScheme = "Bearer";
    public const string ClientIdHeader = "Client-Id";

    public static readonly string[] RequiredScopes =
    [
        "moderator:read:followers",
        "channel:read:subscriptions",
        "channel:read:redemptions",
        "bits:read",
        "user:read:chat",
        "user:write:chat"
    ];

    public static string BuildStreamsUrl(string userId)
    {
        return $"{StreamsUrl}?user_id={Uri.EscapeDataString(userId)}";
    }

    public static class FormFields
    {
        public const string ClientId = "client_id";
        public const string ClientSecret = "client_secret";
        public const string DeviceCode = "device_code";
        public const string GrantType = "grant_type";
        public const string RefreshToken = "refresh_token";
        public const string Scopes = "scopes";
    }

    public static class GrantTypes
    {
        public const string DeviceCode = "urn:ietf:params:oauth:grant-type:device_code";
        public const string RefreshToken = "refresh_token";
    }

    public static class OAuthErrors
    {
        public const string AuthorizationPending = "authorization_pending";
        public const string SlowDown = "slow_down";
    }

    public static class Json
    {
        public const string Data = "data";
        public const string Error = "error";
        public const string Id = "id";
        public const string Login = "login";
        public const string DisplayName = "display_name";
        public const string ProfileImageUrl = "profile_image_url";
        public const string ViewerCount = "viewer_count";
        public const string Title = "title";
        public const string GameName = "game_name";
        public const string DeviceCode = "device_code";
        public const string UserCode = "user_code";
        public const string VerificationUri = "verification_uri";
        public const string ExpiresIn = "expires_in";
        public const string Interval = "interval";
        public const string AccessToken = "access_token";
        public const string RefreshToken = "refresh_token";
        public const string Scope = "scope";
    }
}
