using NeoTwitch.Models;
using NeoTwitch.ViewModels.Obs;

namespace NeoTwitch.Services.Obs;

public static class ObsSceneViewService
{
    private const int DefaultShortNameLength = 24;

    public static IReadOnlyList<ObsSceneRow> BuildRows(
        IEnumerable<ObsSceneInfo> scenes,
        string? currentScene,
        int shortNameLength = DefaultShortNameLength)
    {
        var maxLength = Math.Max(1, shortNameLength);
        var current = currentScene ?? string.Empty;

        return scenes
            .Where(scene => !string.IsNullOrWhiteSpace(scene.Name))
            .Select(scene => new ObsSceneRow(
                scene.Name,
                string.Equals(scene.Name, current, StringComparison.OrdinalIgnoreCase),
                Shorten(scene.Name, maxLength)))
            .ToList();
    }

    public static IReadOnlyList<ObsSceneChoice> BuildChoices(IEnumerable<ObsSceneRow> rows, string keepCurrentLabel)
    {
        return new[] { new ObsSceneChoice("", keepCurrentLabel) }
            .Concat(rows.Select(scene => new ObsSceneChoice(scene.Name, scene.Name)))
            .ToList();
    }

    public static string ResolveSelectedSceneName(string? selected, IEnumerable<ObsSceneChoice> choices)
    {
        var selectedName = selected ?? string.Empty;
        return choices.Any(choice => string.Equals(choice.Name, selectedName, StringComparison.OrdinalIgnoreCase))
            ? selectedName
            : string.Empty;
    }

    private static string Shorten(string value, int maxLength)
    {
        return value.Length > maxLength
            ? $"{value[..maxLength]}..."
            : value;
    }
}
