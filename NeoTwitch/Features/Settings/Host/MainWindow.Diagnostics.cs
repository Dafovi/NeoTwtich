using NeoTwitch.Services;
using NeoTwitch.Services.Diagnostics;
using NeoTwitch.Services.Status;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.Views;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Status;
using static NeoTwitch.Services.Ui.UiBrushFactory;

namespace NeoTwitch;

public partial class MainWindow
{
    private async void RunDiagnostics()
    {
        try
        {
            SaveEditableStateFromFields();
            SaveConfig();

            var result = await BuildDiagnosticsReportAsync();
            AddLog(
                result.WarningCount == 0
                    ? _text.Get(Services.Text.UiTextKeys.DiagnosticsSuccessLog)
                    : _text.Format(Services.Text.UiTextKeys.DiagnosticsWarningsLog, result.WarningCount),
                result.WarningCount == 0 ? ActivityLogKind.Info : ActivityLogKind.Important);
            UpdateSettingsAppState(result.WarningCount == 0
                ? ConnectionVisualState.Connected
                : ConnectionVisualState.Warning);

            ShowDiagnosticsReport(result);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, _text.Get(UiTextKeys.DiagnosticsFailureCrash));
            AddLog(_text.Format(UiTextKeys.DiagnosticsFailureLog, ex.Message), ActivityLogKind.Important);
            UpdateSettingsAppState(ConnectionVisualState.Disconnected);
            _dialog.ShowWarning(_text.Get(UiTextKeys.DiagnosticsTitle), ex.Message);
        }
    }

    private void UpdateSettingsAppState(ConnectionVisualState state)
    {
        var labels = new AppStateLabels(
            _text.Get(UiTextKeys.AppStateOk),
            _text.Get(UiTextKeys.AppStateWarning),
            _text.Get(UiTextKeys.AppStateError));
        var (text, color, imagePath) = ConnectionStateService.GetAppStateVisual(state, labels);

        _settingsViewModel.UpdateAppState(text, FrozenBrushFrom(color), imagePath);
    }

    private void ShowDiagnosticsReport(DiagnosticResult result)
    {
        var window = new DiagnosticsReportWindow(
            result,
            _text,
            () => AddLog(_text.Get(Services.Text.UiTextKeys.DiagnosticsCopiedLog)))
        {
            Owner = this,
            Icon = Icon
        };
        window.ShowDialog();
    }

    private async Task<DiagnosticResult> BuildDiagnosticsReportAsync()
    {
        var context = new DiagnosticReportContext(
            _config,
            _settingsStore.SettingsPath,
            _settingsStore.BackupDirectory,
            _eventSubClient.IsRunning,
            _streamStatus,
            _lightController.HasOpenPort,
            _lightController.CurrentPort,
            _lightController.AckStatusText,
            RuleHasValidAudio);
        return await _diagnosticReportService.BuildAsync(context);
    }
}
