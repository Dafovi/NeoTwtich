namespace NeoTwitch.ViewModels.Library;

public sealed record AudioGroupChoice(string Id, string Name)
{
    public override string ToString() => Name;
}
