using System.Windows;
using System.Windows.Controls;
using NeoTwitch.Models;
using NeoTwitch.ViewModels.Activity;
using WpfMessageBox = System.Windows.MessageBox;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void ObsOverlaySettingsChanged(object sender, RoutedEventArgs e)
    {
        SaveObsOverlaySettings();
    }

    internal void ObsOverlaySettingsChanged(object sender, TextChangedEventArgs e)
    {
        SaveObsOverlaySettings();
    }

    internal void ObsOverlaySettingsChanged(object sender, SelectionChangedEventArgs e)
    {
        SaveObsOverlaySettings();
    }

    internal void CopyObsOverlayUrlButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var url = BuildObsOverlayUrl();
            System.Windows.Clipboard.SetText(url);
            AddLog("OBS: enlace de overlay copiado.", ActivityLogKind.Obs);
        }
        catch (Exception ex)
        {
            AddLog($"OBS overlay: {ex.Message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "OBS", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SaveObsOverlaySettings()
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        SaveConfig();
        UpdateObsOverlayFields();
    }

    private void UpdateObsOverlayFields()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateObsOverlayFields);
            return;
        }

        if (_initializingComponent)
        {
            return;
        }

        ObsOverlayUrlBox.Text = BuildObsOverlayUrl();
        var customPosition = string.Equals(_config.Obs.OverlayPositionMode, "Custom", StringComparison.OrdinalIgnoreCase);
        ObsOverlayXBox.IsEnabled = customPosition;
        ObsOverlayYBox.IsEnabled = customPosition;
        ObsOverlayXBox.Opacity = customPosition ? 1d : 0.58d;
        ObsOverlayYBox.Opacity = customPosition ? 1d : 0.58d;
    }

    private string BuildObsOverlayUrl()
    {
        return _obsOverlayService.BuildOverlayUrl();
    }

    private void WriteObsOverlayState(MediaAssetConfig asset, ObsMediaKind kind, TimeSpan duration)
    {
        try
        {
            _obsOverlayService.WriteState(asset, kind, _config.Obs, duration);
        }
        catch (Exception ex)
        {
            AddLog($"OBS overlay: {ex.Message}", ActivityLogKind.Important);
        }
    }

    private void ClearObsOverlayState()
    {
        try
        {
            _obsOverlayService.ClearState();
        }
        catch (Exception ex)
        {
            AddLog($"OBS overlay: {ex.Message}", ActivityLogKind.Important);
        }
    }
}
