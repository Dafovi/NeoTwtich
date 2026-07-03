using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    private async void PreviewAudio(object? parameter)
    {
        if (parameter is not string audioId)
        {
            return;
        }

        var audio = _config.AudioLibrary.FirstOrDefault(item => string.Equals(item.Id, audioId, StringComparison.OrdinalIgnoreCase));
        if (audio is null)
        {
            return;
        }

        if (_audioPreviewPlayback is not null && string.Equals(_previewingAudioId, audio.Id, StringComparison.OrdinalIgnoreCase))
        {
            _audioPreviewPlayback.Stop();
            ClearAudioPreviewState(audio.Id);
            return;
        }

        var playback = await _audioPlayer.PrepareAsync(audio.FilePath, _config.AlertVolumePercent, AddLog);
        if (playback is null)
        {
            return;
        }

        _audioPreviewPlayback?.Stop();
        _audioPreviewPlayback = playback;
        _previewingAudioId = audio.Id;
        MarkAudioAssetUsed(audio, playback.Duration);
        playback.Play();
        AddLog(_text.Format(UiTextKeys.AudioPlayingLog, audio.DisplayName), ActivityLogKind.Audio);
        _ = WatchAudioPreviewCompletionAsync(playback, audio.Id);
    }

    private void DeleteAudio(object? parameter)
    {
        if (parameter is not string audioId)
        {
            return;
        }

        var audio = _config.AudioLibrary.FirstOrDefault(item => string.Equals(item.Id, audioId, StringComparison.OrdinalIgnoreCase));
        if (audio is null)
        {
            return;
        }

        if (!_dialog.Confirm(_text.Get(UiTextKeys.AudioTitle), _text.Format(UiTextKeys.LibraryDeleteAssetPrompt, audio.DisplayName)))
        {
            return;
        }

        if (string.Equals(_previewingAudioId, audio.Id, StringComparison.OrdinalIgnoreCase))
        {
            _audioPreviewPlayback?.Stop();
            ClearAudioPreviewState(audio.Id);
        }

        _config.AudioLibrary.Remove(audio);
        foreach (var rule in _config.Rules.Where(rule => string.Equals(rule.AudioAssetId, audio.Id, StringComparison.OrdinalIgnoreCase)))
        {
            rule.AudioAssetId = "";
            rule.AudioPath = "";
            rule.PlayAudio = rule.AudioSourceMode == AudioSourceMode.Group && !string.IsNullOrWhiteSpace(rule.AudioGroupId);
        }

        SaveConfig();
        RefreshAudioLibraryView();
        RefreshRulesView();
        LoadSelectedRuleIntoUi();
    }

    private async Task WatchAudioPreviewCompletionAsync(AudioPlayback playback, string audioId)
    {
        try
        {
            await playback.Completion;
        }
        finally
        {
            await Dispatcher.InvokeAsync(() => ClearAudioPreviewState(audioId));
        }
    }

    private void ClearAudioPreviewState(string audioId)
    {
        if (!string.Equals(_previewingAudioId, audioId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _audioPreviewPlayback = null;
        _previewingAudioId = "";
        RefreshAudioLibraryView();
    }

    private void StopAudioPreview()
    {
        if (_audioPreviewPlayback is null)
        {
            return;
        }

        var audioId = _previewingAudioId;
        _audioPreviewPlayback.Stop();
        ClearAudioPreviewState(audioId);
    }

    private bool RuleHasValidAudio(EventRule rule)
    {
        if (!Dispatcher.CheckAccess())
        {
            return Dispatcher.Invoke(() => RuleHasValidAudio(rule));
        }

        return AudioRuleAssetService.HasValidAudio(rule, _config.AudioLibrary, _audioRandom);
    }

    private AudioAssetConfig? ResolveRuleAudioAsset(EventRule rule)
    {
        if (!Dispatcher.CheckAccess())
        {
            return Dispatcher.Invoke(() => ResolveRuleAudioAsset(rule));
        }

        return AudioRuleAssetService.ResolveRuleAudioAsset(rule, _config.AudioLibrary, _audioRandom);
    }

    private void MarkAudioAssetUsed(AudioAssetConfig audio, TimeSpan? duration)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => MarkAudioAssetUsed(audio, duration));
            return;
        }

        if (duration is { TotalMilliseconds: > 0 })
        {
            audio.DurationMs = (int)Math.Round(duration.Value.TotalMilliseconds);
        }

        audio.LastUsedAt = DateTimeOffset.Now;
        SaveConfig();
        RefreshAudioLibraryView();
    }
}
