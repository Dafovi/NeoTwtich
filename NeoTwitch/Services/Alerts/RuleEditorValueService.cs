using NeoTwitch.Models;
using NeoTwitch.Services.Text;

namespace NeoTwitch.Services.Alerts;

public static class RuleEditorValueService
{
    public static string ResolveRuleName(string? editorText, string? existingName, TwitchEventKind kind)
    {
        return ResolveRuleName(editorText, existingName, kind, UiTextService.CreateDefault());
    }

    public static string ResolveRuleName(string? editorText, string? existingName, TwitchEventKind kind, IUiTextService text)
    {
        var editorValue = (editorText ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(editorValue))
        {
            return editorValue;
        }

        var currentName = (existingName ?? "").Trim();
        return string.IsNullOrWhiteSpace(currentName)
            ? DisplayNameService.For(kind, text)
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
