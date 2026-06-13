namespace NeoTwitch.ViewModels.Obs;

public sealed record ObsSceneRestoreRequest(
    string PreviousScene,
    string TargetScene,
    TimeSpan Delay,
    DateTimeOffset StartedAt);
