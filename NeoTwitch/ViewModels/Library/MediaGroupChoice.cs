namespace NeoTwitch.ViewModels.Library;

public sealed record MediaGroupChoice(string Id, string Name)
{
    public override string ToString() => Name;
}
