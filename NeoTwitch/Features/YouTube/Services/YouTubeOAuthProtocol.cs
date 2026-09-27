namespace NeoTwitch.Services.YouTube;

public static class YouTubeOAuthProtocol
{
    public const string AuthorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    public const string TokenUrl = "https://oauth2.googleapis.com/token";
    public const string YouTubeReadOnlyScope = "https://www.googleapis.com/auth/youtube.readonly";
    public const string YouTubeChatWriteScope = "https://www.googleapis.com/auth/youtube.force-ssl";

    public static readonly string[] RequiredScopes = [YouTubeReadOnlyScope, YouTubeChatWriteScope];
}
