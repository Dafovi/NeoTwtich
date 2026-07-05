namespace NeoTwitch.Models;

public sealed record ObsSceneInfo(string Name);

public sealed record ObsConnectionResult(
    bool Connected,
    string Version,
    string CurrentScene,
    bool StudioMode,
    IReadOnlyList<ObsSceneInfo> Scenes);
