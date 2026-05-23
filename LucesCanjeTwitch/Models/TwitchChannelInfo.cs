namespace LucesCanjeTwitch.Models;

public sealed class TwitchChannelInfo
{
    public string UserId { get; set; } = "";
    public string Login { get; set; } = "";
    public string DisplayName { get; set; } = "";

    public bool IsReady => !string.IsNullOrWhiteSpace(UserId);
}
