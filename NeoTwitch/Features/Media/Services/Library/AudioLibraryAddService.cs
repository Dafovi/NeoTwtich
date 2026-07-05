using System.IO;
using NeoTwitch.Models;

namespace NeoTwitch.Services.Library;

public sealed record AudioAssetAddRequest(
    string FilePath,
    string Name,
    string GroupId,
    string RuleId);

public sealed record AudioAssetAddResult(AudioAssetConfig? Asset, EventRule? LinkedRule, bool Created)
{
    public bool Saved => Asset is not null;
}

public static class AudioLibraryAddService
{
    public static async Task<AudioAssetAddResult> AddOrUpdateAsync(
        AppConfig config,
        AudioAssetAddRequest request,
        Func<string, Task<TimeSpan?>> probeDurationAsync,
        Func<string, bool>? fileExists = null)
    {
        fileExists ??= File.Exists;
        var path = (request.FilePath ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(path) || !fileExists(path))
        {
            return new AudioAssetAddResult(null, null, false);
        }

        var existing = config.AudioLibrary.FirstOrDefault(audio =>
            string.Equals(audio.FilePath, path, StringComparison.OrdinalIgnoreCase));
        var created = existing is null;
        var audio = existing ?? new AudioAssetConfig { FilePath = path };
        audio.Name = string.IsNullOrWhiteSpace(request.Name)
            ? Path.GetFileNameWithoutExtension(path)
            : request.Name.Trim();
        audio.GroupId = request.GroupId ?? string.Empty;

        var duration = await probeDurationAsync(path);
        if (duration is { TotalMilliseconds: > 0 })
        {
            audio.DurationMs = (int)Math.Round(duration.Value.TotalMilliseconds);
        }

        if (created)
        {
            config.AudioLibrary.Add(audio);
        }

        var linkedRule = LinkRule(config, audio, request.RuleId);
        return new AudioAssetAddResult(audio, linkedRule, created);
    }

    private static EventRule? LinkRule(AppConfig config, AudioAssetConfig audio, string ruleId)
    {
        var rule = config.Rules.FirstOrDefault(item => string.Equals(item.Id, ruleId, StringComparison.OrdinalIgnoreCase));
        if (rule is null)
        {
            return null;
        }

        rule.PlayAudio = true;
        rule.AudioSourceMode = AudioSourceMode.Single;
        rule.AudioAssetId = audio.Id;
        rule.AudioGroupId = "";
        rule.AudioPath = audio.FilePath;
        return rule;
    }
}
