using System.Windows;
using System.Windows.Controls;
using NeoTwitch.Models;
using NeoTwitch.ViewModels.Activity;

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

    private void CopyObsOverlayUrl()
    {
        try
        {
            var url = BuildObsOverlayUrl();
            _clipboard.SetText(url);
            AddLog("OBS: enlace de overlay copiado.", ActivityLogKind.Obs);
        }
        catch (Exception ex)
        {
            AddLog($"OBS overlay: {ex.Message}", ActivityLogKind.Important);
            _dialog.ShowWarning("OBS", ex.Message);
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

        _obsViewModel.UpdateOverlayUrl(BuildObsOverlayUrl());
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
