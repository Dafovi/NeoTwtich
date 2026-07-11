using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NeoTwitch.Models;
using NeoTwitch.Services.Dashboard;
using NeoTwitch.Services;
using NeoTwitch.Services.Status;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;
using Forms = System.Windows.Forms;
using DrawingIcon = System.Drawing.Icon;
using static NeoTwitch.Services.Text.UiTextFormatter;
using static NeoTwitch.Services.Ui.UiBrushFactory;

namespace NeoTwitch;

public partial class MainWindow
{
    private void UpdateConnectionButtons()
    {
        var labels = GetConnectionButtonLabels();
        _connectionsViewModel.UpdateButtonStates(
            ConnectionButtonStateService.ResolveTwitch(
                _isTwitchAuthorizing,
                _isTwitchConnecting,
                _eventSubClient.IsRunning,
                labels),
            ConnectionButtonStateService.ResolveArduino(
                _config.ArduinoEnabled,
                _isArduinoConnecting,
                labels),
            ConnectionButtonStateService.ResolveAlexa(
                _config.Alexa.Enabled,
                _isAlexaConnecting,
                labels),
            ConnectionButtonStateService.ResolveObs(
                _config.Obs.Enabled,
                _isObsConnecting,
                _isObsSceneActionRunning,
                _obsService.IsConnected,
                labels),
            ConnectionButtonStateService.ResolveObsTest(
                _config.Obs.Enabled,
                _isObsConnecting,
                _isObsSceneActionRunning,
                labels));
    }

    private ConnectionButtonLabels GetConnectionButtonLabels()
    {
        return new ConnectionButtonLabels(
            _text.Get(UiTextKeys.ConnectionButtonTwitchAuthorizing),
            _text.Get(UiTextKeys.ConnectionButtonConnecting),
            _text.Get(UiTextKeys.ConnectionButtonTwitchDisconnect),
            _text.Get(UiTextKeys.ConnectionButtonTwitchConnect),
            _text.Get(UiTextKeys.ConnectionButtonArduinoConnect),
            _text.Get(UiTextKeys.ConnectionButtonAlexaTesting),
            _text.Get(UiTextKeys.ConnectionButtonAlexaTest),
            _text.Get(UiTextKeys.ConnectionButtonObsDisconnect),
            _text.Get(UiTextKeys.ConnectionButtonObsConnect),
            _text.Get(UiTextKeys.ConnectionButtonObsScenesUpdating),
            _text.Get(UiTextKeys.ConnectionButtonObsScenesRefresh));
    }

    private void UpdateTwitchLiveIndicator()
    {
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        _shellViewModel.UpdateLiveIndicator(
            _streamStatus is { IsLive: true },
            palette,
            _text.Get(UiTextKeys.TwitchLive),
            _text.Get(UiTextKeys.TwitchOffline),
            _text.Get(UiTextKeys.TwitchProfile));
    }

    private void UpdateChannelAvatar()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_config.Channel.ProfileImageUrl))
            {
                ChannelAvatarImage.Source = new BitmapImage(new Uri(_config.Channel.ProfileImageUrl, UriKind.Absolute));
                return;
            }
        }
        catch
        {
            // Use the bundled app icon when Twitch has no image available.
        }

        ChannelAvatarImage.Source = new BitmapImage(new Uri(NeoTwitchProduct.AppIconPackUri, UriKind.Absolute));
    }

    private void UpdateSliderLabels()
    {
        var brightnessPercent = _alertsViewModel.Editor.BrightnessPercent;
        var backgroundBrightnessPercent = _lightsViewModel.BackgroundBrightnessPercent;

        _updatingLightValueFields = true;
        try
        {
            DurationValueText.Text = ((int)Math.Round(_alertsViewModel.Editor.DurationMs)).ToString();
            CycleValueText.Text = ((int)Math.Round(_alertsViewModel.Editor.CycleMs)).ToString();
            StepValueText.Text = ((int)Math.Round(_alertsViewModel.Editor.StepMs)).ToString();
            VirtualDurationValueText.Text = ((int)Math.Round(_alertsViewModel.Editor.VirtualLightsDurationMs)).ToString();
            VirtualCycleValueText.Text = ((int)Math.Round(_alertsViewModel.Editor.VirtualLightsCycleMs)).ToString();
            VirtualStepValueText.Text = ((int)Math.Round(_alertsViewModel.Editor.VirtualLightsStepMs)).ToString();
            VirtualObsOpacityValueText.Text = ((int)Math.Round(_alertsViewModel.Editor.VirtualLightsObsOpacity)).ToString();
            VirtualScreenPixelValueText.Text = ((int)Math.Round(_alertsViewModel.Editor.VirtualLightsScreenPixelSize)).ToString();
            VirtualScreenSaturationValueText.Text = ((int)Math.Round(_alertsViewModel.Editor.VirtualLightsScreenSaturation)).ToString();
            BackgroundCycleValueText.Text = ((int)Math.Round(_lightsViewModel.BackgroundCycleMs)).ToString();
            BackgroundStepValueText.Text = ((int)Math.Round(_lightsViewModel.BackgroundStepMs)).ToString();
        }
        finally
        {
            _updatingLightValueFields = false;
        }

        UpdateCircularProgress(BackgroundBrightnessArc, backgroundBrightnessPercent / 100d);
    }

    private static void UpdateCircularProgress(System.Windows.Shapes.Path path, double progress)
    {
        path.Data = CircularProgressGeometryService.BuildArcGeometry(progress);
    }

}
