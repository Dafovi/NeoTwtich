namespace NeoTwitch.Models;

public sealed class TwitchEvent
{
    public TwitchEventKind Kind { get; init; }
    public string Title { get; init; } = "";
    public string? UserName { get; init; }
    public string? RewardTitle { get; init; }
    public int? ViewerCount { get; init; }
    public int? Bits { get; init; }
    public string? Message { get; init; }
    public string? RawType { get; init; }
    public string EventSubMessageId { get; set; } = "";
    public string EventSubSessionId { get; set; } = "";
    public string EventSubMessageType { get; set; } = "";
}
