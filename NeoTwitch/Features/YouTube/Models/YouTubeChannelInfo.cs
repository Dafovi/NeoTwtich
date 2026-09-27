namespace NeoTwitch.Models;

public sealed class YouTubeChannelInfo
{
    public string ChannelId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string ThumbnailUrl { get; set; } = "";

    public bool IsReady => !string.IsNullOrWhiteSpace(ChannelId);
}
