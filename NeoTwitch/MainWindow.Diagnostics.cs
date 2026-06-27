using System.Windows;
using NeoTwitch.Services;
using NeoTwitch.Services.Diagnostics;
using NeoTwitch.Services.Status;
using NeoTwitch.Services.Ui;
using NeoTwitch.Views;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Status;
using WpfMessageBox = System.Windows.MessageBox;
using static NeoTwitch.Services.Ui.UiBrushFactory;

namespace NeoTwitch;

public partial class MainWindow
{
    internal async void RunDiagnosticsButton_Click(object sender, RoutedEventArgs e)
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
            CrashReporter.Log(ex, "No se pudo ejecutar el diagnostico.");
            AddLog($"Diagnostico: {ex.Message}", ActivityLogKind.Important);
            UpdateSettingsAppState(ConnectionVisualState.Disconnected);
            WpfMessageBox.Show(this, ex.Message, "Diagnostico", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateSettingsAppState(ConnectionVisualState state)
    {
        var (text, color, imagePath) = ConnectionStateService.GetAppStateVisual(state);

        SettingsAppStateIcon.Source = PackImageLoader.Load(imagePath);
        SettingsDiagnosticStatusText.Text = text;
        SettingsDiagnosticStatusText.Foreground = FrozenBrushFrom(color);
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
