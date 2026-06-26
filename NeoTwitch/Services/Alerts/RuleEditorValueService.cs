using NeoTwitch.Models;

namespace NeoTwitch.Services.Alerts;

public static class RuleEditorValueService
{
    public static string ResolveRuleName(string? editorText, string? existingName, TwitchEventKind kind)
    {
        var text = (editorText ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var currentName = (existingName ?? "").Trim();
        return string.IsNullOrWhiteSpace(currentName)
            ? DisplayNames.For(kind)
            : currentName;
    }

    public static string ResolveLegacyAudioPath(
        AudioSourceMode sourceMode,
        string? assetId,
        IEnumerable<AudioAssetConfig> audioLibrary)
    {
        if (sourceMode != AudioSourceMode.Single || string.IsNullOrWhiteSpace(assetId))
        {
            return "";
        }

        return audioLibrary.FirstOrDefault(audio =>
            string.Equals(audio.Id, assetId, StringComparison.OrdinalIgnoreCase))?.FilePath ?? "";
    }
}
