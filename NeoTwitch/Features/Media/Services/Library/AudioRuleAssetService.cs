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
        return ResolveRuleAudioAsset(AlertExecutionSnapshotFactory.Create(rule).Audio, library, random, fileExists);
    }

    public static AudioAssetConfig? ResolveRuleAudioAsset(
        AlertAudioActionSnapshot audio,
        IEnumerable<AudioAssetConfig> library,
        Random random,
        Func<string, bool>? fileExists = null)
    {
        if (!audio.Enabled)
        {
            return null;
        }

        var audioLibrary = library.ToArray();
        if (audio.SourceMode == AudioSourceMode.Group)
        {
            fileExists ??= File.Exists;
            var candidates = audioLibrary
                .Where(candidate => string.Equals(candidate.GroupId, audio.GroupId, StringComparison.OrdinalIgnoreCase))
                .Where(candidate => fileExists(candidate.FilePath))
                .ToArray();
            return candidates.Length == 0
                ? null
                : candidates[random.Next(candidates.Length)];
        }

        return audioLibrary.FirstOrDefault(candidate => string.Equals(candidate.Id, audio.AssetId, StringComparison.OrdinalIgnoreCase))
            ?? audioLibrary.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(audio.LegacyPath)
                && string.Equals(candidate.FilePath, audio.LegacyPath, StringComparison.OrdinalIgnoreCase));
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
