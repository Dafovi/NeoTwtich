using NeoTwitch.Models;

namespace NeoTwitch;

public partial class MainWindow
{
    private void SelectRuleAudioMode(object? parameter)
    {
        if (!TryParseEnumParameter<AudioSourceMode>(parameter, out var mode))
        {
            return;
        }

        _alertsViewModel.Editor.AudioSourceMode = mode;
        UpdateRuleAudioModeSelection();
        UpdateRuleOptionVisibility();
        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }
    }

    private void SelectRuleObsMediaKind(object? parameter)
    {
        if (!TryParseEnumParameter<ObsMediaKind>(parameter, out var kind))
        {
            return;
        }

        _alertsViewModel.Editor.ObsMediaKind = kind;
        RefreshRuleObsMediaChoices();
        UpdateRuleObsMediaModeSelection();
        UpdateRuleOptionVisibility();
        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }
    }

    private void SelectRuleObsMediaSourceMode(object? parameter)
    {
        var raw = parameter?.ToString() ?? "";
        var parts = raw.Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2
            && Enum.TryParse<ObsMediaKind>(parts[0], out var kind)
            && Enum.TryParse<MediaSourceMode>(parts[1], out var prefixedMode))
        {
            if (kind == ObsMediaKind.Image)
            {
                _alertsViewModel.Editor.ObsImageSourceMode = prefixedMode;
            }
            else
            {
                _alertsViewModel.Editor.ObsVideoSourceMode = prefixedMode;
            }
        }
        else if (TryParseEnumParameter<MediaSourceMode>(parameter, out var mode))
        {
            _alertsViewModel.Editor.ObsMediaSourceMode = mode;
        }
        else
        {
            return;
        }

        UpdateRuleObsMediaModeSelection();
        UpdateRuleOptionVisibility();
        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }
    }

    private void SelectRuleEventKind(object? parameter)
    {
        if (!TryParseEnumParameter<TwitchEventKind>(parameter, out var kind))
        {
            return;
        }

        _alertsViewModel.Editor.EventKind = kind;
        UpdateEventKindTileSelection();
        UpdateRuleOptionVisibility();
        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }
    }

    private static bool TryParseEnumParameter<T>(object? parameter, out T value)
        where T : struct, Enum
    {
        if (parameter is T typed)
        {
            value = typed;
            return true;
        }

        return Enum.TryParse(parameter?.ToString(), out value);
    }
}
