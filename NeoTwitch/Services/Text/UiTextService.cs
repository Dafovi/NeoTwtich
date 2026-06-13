namespace NeoTwitch.Services.Text;

public sealed class UiTextService : IUiTextService
{
    private readonly IReadOnlyDictionary<string, string> _catalog;

    public UiTextService(IReadOnlyDictionary<string, string> catalog)
    {
        _catalog = catalog;
    }

    public static UiTextService CreateDefault()
    {
        return new UiTextService(SpanishUiTextCatalog.Create());
    }

    public string Get(string key)
    {
        return _catalog.TryGetValue(key, out var value) ? value : key;
    }

    public string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }
}
