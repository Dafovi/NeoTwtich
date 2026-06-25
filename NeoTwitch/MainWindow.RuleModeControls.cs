using System.Windows;
using NeoTwitch.Models;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void RuleAudioModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button
            || button.Tag is not string value
            || !Enum.TryParse<AudioSourceMode>(value, out var mode))
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

    internal void RuleObsMediaKindButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button
            || button.Tag is not string value
            || !Enum.TryParse<ObsMediaKind>(value, out var kind))
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

    internal void RuleObsMediaSourceModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button
            || button.Tag is not string value
            || !Enum.TryParse<MediaSourceMode>(value, out var mode))
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

    internal void EventKindTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button
            || button.Tag is not string value
            || !Enum.TryParse<TwitchEventKind>(value, out var kind))
        {
            return;
        }

        EventKindBox.SelectedValue = kind;
        UpdateEventKindTileSelection();
    }
}
