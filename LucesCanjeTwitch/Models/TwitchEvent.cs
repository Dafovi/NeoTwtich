namespace LucesCanjeTwitch.Models;

public sealed class TwitchEvent
{
    public TwitchEventKind Kind { get; init; }
    public string Title { get; init; } = "";
    public string? UserName { get; init; }
    public string? RewardTitle { get; init; }
    public int? ViewerCount { get; init; }
    public string? RawType { get; init; }
}
