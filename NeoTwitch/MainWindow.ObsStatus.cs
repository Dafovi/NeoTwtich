using NeoTwitch.Models;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Obs;
using NeoTwitch.Services.Status;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch;

public partial class MainWindow
{
    private void UpdateObsStatusText()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateObsStatusText);
            return;
        }

        if (_initializingComponent)
        {
            return;
        }

        var status = ObsStatusTextService.Build(
            _config.Obs.Enabled,
            _isObsConnecting,
            _obsService.IsConnected,
            _obsConnectionError,
            _obsService.CurrentScene,
            _config.Obs.Host,
            _config.Obs.Port,
            _obsService.Version,
            _obsService.Scenes.Count,
            _obsService.StudioMode,
            GetObsStatusTextLabels());

        ObsStatusText.Text = status.StatusText;
        _connectionsViewModel.UpdateObsConnectionHelpText(status.StatusText);
        UpdateObsOverlayFields();

        ObsConnectionStateText.Text = status.State;
        ObsCurrentSceneText.Text = status.CurrentScene;
        ObsHostSummaryText.Text = status.Host;
        ObsPortSummaryText.Text = status.Port;
        ObsVersionText.Text = status.Version;
        ObsSceneCountText.Text = status.SceneCount;
        ObsStudioModeText.Text = status.StudioMode;
        ObsScenesList.IsEnabled = _config.Obs.Enabled
            && _obsService.IsConnected
            && !_isObsConnecting
            && !_isObsSceneActionRunning;
        ObsScenesList.Opacity = ObsScenesList.IsEnabled ? 1d : 0.58d;

        RefreshMediaLibraryView(MediaLibraryKind.Image);
        RefreshMediaLibraryView(MediaLibraryKind.Video);
        UpdateConnectionButtons();
        RefreshDashboardConnectionStates();
    }

    private ObsStatusTextLabels GetObsStatusTextLabels()
    {
        return new ObsStatusTextLabels(
            _text.Get(UiTextKeys.ConnectionDisabled),
            _text.Get(UiTextKeys.ConnectionConnecting),
            _text.Get(UiTextKeys.ConnectionConnected),
            _text.Get(UiTextKeys.ConnectionDisconnected),
            _text.Get(UiTextKeys.ObsReviewConnection),
            _text.Get(UiTextKeys.ObsDisabledStatusText),
            _text.Get(UiTextKeys.ObsConnectedStatusText),
            _text.Get(UiTextKeys.ObsConnectPromptStatusText),
            _text.Get(UiTextKeys.ObsNoScene),
            _text.Get(UiTextKeys.ObsDefaultHost),
            _text.Get(UiTextKeys.ObsNoVersion),
            _text.Get(UiTextKeys.ObsStudioModeEnabled),
            _text.Get(UiTextKeys.ObsStudioModeDisabled));
    }

    private void ApplyObsResult(ObsConnectionResult result)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => ApplyObsResult(result));
            return;
        }

        _obsConnectionError = "";
        _obsSceneRows.Clear();
        foreach (var scene in ObsSceneViewService.BuildRows(result.Scenes, result.CurrentScene))
        {
            _obsSceneRows.Add(scene);
        }

        RefreshObsSceneChoices();

        if (RulesList.SelectedItem is EventRule rule
            && !string.IsNullOrWhiteSpace(rule.ObsSceneName)
            && _obsSceneRows.Any(scene => string.Equals(scene.Name, rule.ObsSceneName, StringComparison.OrdinalIgnoreCase)))
        {
            RuleObsSceneBox.SelectedValue = rule.ObsSceneName;
        }

        RefreshRulesView();
        UpdateRuleOptionVisibility();
        UpdateObsStatusText();
    }

    private string ObsConnectionSignature()
    {
        return $"{_config.Obs.Enabled}|{_config.Obs.Host.Trim()}|{_config.Obs.Port}|{_config.Obs.Password}";
    }

    private void RefreshObsSceneChoices()
    {
        var selected = RuleObsSceneBox.SelectedValue as string ?? "";
        var choices = ObsSceneViewService.BuildChoices(_obsSceneRows, _text.Get(UiTextKeys.ObsKeepCurrentScene));
        _obsSceneChoices.Clear();
        foreach (var choice in choices)
        {
            _obsSceneChoices.Add(choice);
        }

        RuleObsSceneBox.SelectedValue = ObsSceneViewService.ResolveSelectedSceneName(selected, _obsSceneChoices);
    }
}
