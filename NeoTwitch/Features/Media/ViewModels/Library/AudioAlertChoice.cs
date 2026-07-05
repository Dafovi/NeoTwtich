namespace NeoTwitch.ViewModels.Library;

public sealed record AudioAlertChoice(string Id, string Name)
{
    public override string ToString() => Name;
}
