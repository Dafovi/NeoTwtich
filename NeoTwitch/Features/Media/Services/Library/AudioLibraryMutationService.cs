using NeoTwitch.Models;

namespace NeoTwitch.Services.Library;

public sealed record AudioAssetRemovalResult(AudioAssetConfig? RemovedAsset, int UpdatedRuleCount)
{
    public bool Removed => RemovedAsset is not null;
}

public static class AudioLibraryMutationService
{
    public static AudioAssetRemovalResult RemoveAudioAsset(AppConfig config, string audioId)
    {
        var audio = config.AudioLibrary.FirstOrDefault(item => string.Equals(item.Id, audioId, StringComparison.OrdinalIgnoreCase));
        if (audio is null)
        {
            return new AudioAssetRemovalResult(null, 0);
        }

        config.AudioLibrary.Remove(audio);

        var updatedRules = 0;
        foreach (var rule in config.Rules.Where(rule => string.Equals(rule.AudioAssetId, audio.Id, StringComparison.OrdinalIgnoreCase)))
        {
            rule.AudioAssetId = "";
            rule.AudioPath = "";
            rule.PlayAudio = rule.AudioSourceMode == AudioSourceMode.Group && !string.IsNullOrWhiteSpace(rule.AudioGroupId);
            updatedRules++;
        }

        return new AudioAssetRemovalResult(audio, updatedRules);
    }
}
