namespace NeoTwitch.Services.Text;

public static class UiTextFormatter
{
    public static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }

    public static string FormatNameList(IReadOnlyList<string> names, int maxVisible = 5, string emptyText = "sin nombre")
    {
        var validNames = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
        var visibleNames = validNames
            .Take(maxVisible)
            .ToArray();

        var text = visibleNames.Length == 0
            ? emptyText
            : string.Join(", ", visibleNames);
        var remaining = validNames.Length - visibleNames.Length;
        return remaining > 0 ? $"{text} y {remaining} mas" : text;
    }

    public static string NormalizeEventName(string? text, string fallback)
    {
        return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
    }

    public static string BuildSecretMask(string? value, int minLength = 8, int maxLength = 20)
    {
        var length = Math.Clamp((value ?? "").Trim().Length, minLength, maxLength);
        return new string('*', length);
    }
}
