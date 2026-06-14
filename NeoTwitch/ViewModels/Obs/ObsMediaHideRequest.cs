namespace NeoTwitch.ViewModels.Obs;

public sealed record ObsMediaHideRequest(
    string SceneName,
    string SourceName,
    TimeSpan Duration,
    DateTimeOffset StartedAt);
