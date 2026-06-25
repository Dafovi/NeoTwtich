namespace NeoTwitch.ViewModels.Ui;

public sealed record UiOption<T>(string Label, T Value)
{
    public override string ToString() => Label;
}
