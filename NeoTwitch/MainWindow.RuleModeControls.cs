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

        _ruleAudioMode = mode;
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

        RuleObsMediaKindBox.SelectedValue = kind;
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
        if (!TryParseEnumParameter<MediaSourceMode>(parameter, out var mode))
        {
            return;
        }

        RuleObsMediaSourceModeBox.SelectedValue = mode;
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
