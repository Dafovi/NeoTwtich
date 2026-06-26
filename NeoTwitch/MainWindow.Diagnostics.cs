using System.Windows;
using System.Windows.Controls;
using NeoTwitch.Services;
using NeoTwitch.Services.Diagnostics;
using NeoTwitch.Services.Status;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Status;
using WpfClipboard = System.Windows.Clipboard;
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
                    ? "Diagnostico: sin advertencias."
                    : $"Diagnostico: {result.WarningCount} punto(s) por revisar.",
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
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        var reportBox = new System.Windows.Controls.TextBox
        {
            Text = result.Report,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new System.Windows.Media.FontFamily("Cascadia Mono, Consolas"),
            FontSize = 12,
            Background = palette.Input,
            Foreground = palette.Text,
            BorderBrush = palette.Border,
            Margin = new Thickness(0, 12, 0, 12)
        };

        var title = new TextBlock
        {
            Text = result.WarningCount == 0
                ? "Diagnostico sin advertencias"
                : $"Diagnostico con {result.WarningCount} punto(s) por revisar",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = palette.Text
        };

        var copyButton = new System.Windows.Controls.Button
        {
            Content = "Copiar reporte",
            Style = (Style)FindResource("PrimaryButton")
        };
        copyButton.Click += (_, _) =>
        {
            WpfClipboard.SetText(result.Report);
            AddLog("Diagnostico copiado al portapapeles.");
        };

        var closeButton = new System.Windows.Controls.Button
        {
            Content = "Cerrar"
        };

        var buttons = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
        buttons.Children.Add(copyButton);
        buttons.Children.Add(closeButton);

        var layout = new Grid
        {
            Margin = new Thickness(18)
        };
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.Children.Add(title);
        Grid.SetRow(reportBox, 1);
        layout.Children.Add(reportBox);
        Grid.SetRow(buttons, 2);
        layout.Children.Add(buttons);

        var window = new Window
        {
            Owner = this,
            Title = "Diagnostico Neo Twitch",
            Width = 780,
            Height = 620,
            MinWidth = 560,
            MinHeight = 420,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = palette.Window,
            Icon = Icon,
            Content = layout
        };

        closeButton.Click += (_, _) => window.Close();
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
        var service = new DiagnosticReportService(_updateService);
        return await service.BuildAsync(context);
    }
}
