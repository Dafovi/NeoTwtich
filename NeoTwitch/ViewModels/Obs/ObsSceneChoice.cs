namespace NeoTwitch.ViewModels.Obs;

public sealed record ObsSceneChoice(string Name, string Label)
{
    public override string ToString() => Label;
}
