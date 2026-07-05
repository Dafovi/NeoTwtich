using System.IO;
using NeoTwitch.Models;

namespace NeoTwitch.Services.Library;

public static class AudioRuleAssetService
{
    public static AudioAssetConfig? ResolveRuleAudioAsset(
        EventRule rule,
        IEnumerable<AudioAssetConfig> library,
        Random random,
        Func<string, bool>? fileExists = null)
    {
        if (!rule.PlayAudio)
        {
            return null;
        }

        var audioLibrary = library.ToArray();
        if (rule.AudioSourceMode == AudioSourceMode.Group)
        {
            fileExists ??= File.Exists;
            var candidates = audioLibrary
                .Where(audio => string.Equals(audio.GroupId, rule.AudioGroupId, StringComparison.OrdinalIgnoreCase))
                .Where(audio => fileExists(audio.FilePath))
                .ToArray();
            return candidates.Length == 0
                ? null
                : candidates[random.Next(candidates.Length)];
        }

        return audioLibrary.FirstOrDefault(audio => string.Equals(audio.Id, rule.AudioAssetId, StringComparison.OrdinalIgnoreCase))
            ?? audioLibrary.FirstOrDefault(audio => !string.IsNullOrWhiteSpace(rule.AudioPath)
                && string.Equals(audio.FilePath, rule.AudioPath, StringComparison.OrdinalIgnoreCase));
    }

    public static bool HasValidAudio(
        EventRule rule,
        IEnumerable<AudioAssetConfig> library,
        Random random,
        Func<string, bool>? fileExists = null)
    {
        fileExists ??= File.Exists;
        var asset = ResolveRuleAudioAsset(rule, library, random, fileExists);
        if (asset is not null)
        {
            return fileExists(asset.FilePath);
        }

        return rule.AudioSourceMode == AudioSourceMode.Single
            && !string.IsNullOrWhiteSpace(rule.AudioPath)
            && fileExists(rule.AudioPath);
    }

    public static bool RuleUsesAudioAsset(EventRule rule, AudioAssetConfig audio)
    {
        if (!rule.PlayAudio)
        {
            return false;
        }

        if (rule.AudioSourceMode == AudioSourceMode.Group)
        {
            return !string.IsNullOrWhiteSpace(rule.AudioGroupId)
                && string.Equals(rule.AudioGroupId, audio.GroupId, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(rule.AudioAssetId, audio.Id, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(rule.AudioPath)
                && string.Equals(rule.AudioPath, audio.FilePath, StringComparison.OrdinalIgnoreCase));
    }
}
