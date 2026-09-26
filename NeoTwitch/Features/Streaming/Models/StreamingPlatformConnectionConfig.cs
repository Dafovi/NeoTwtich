namespace NeoTwitch.Models;

/// <summary>
/// Provider-neutral connection preferences. Provider-specific secrets remain in protected storage.
/// </summary>
public sealed class StreamingPlatformConnectionConfig
{
    public StreamingPlatform Platform { get; set; } = StreamingPlatform.Twitch;

    public bool IsEnabled { get; set; } = true;

    public bool AutoConnect { get; set; } = true;
}
