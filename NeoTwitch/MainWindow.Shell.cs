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
        ApplyButtonState(TwitchButton, ConnectionButtonStateService.ResolveTwitch(
            _isTwitchAuthorizing,
            _isTwitchConnecting,
            _eventSubClient.IsRunning,
            labels));
        ApplyButtonState(ConnectArduinoButton, ConnectionButtonStateService.ResolveArduino(
            _config.ArduinoEnabled,
            _isArduinoConnecting,
            labels));
        ApplyButtonState(TestAlexaButton, ConnectionButtonStateService.ResolveAlexa(
            _config.Alexa.Enabled,
            _isAlexaConnecting,
            labels));
        ApplyButtonState(ConnectObsButton, ConnectionButtonStateService.ResolveObs(
            _config.Obs.Enabled,
            _isObsConnecting,
            _isObsSceneActionRunning,
            _obsService.IsConnected,
            labels));
        ApplyButtonState(TestObsButton, ConnectionButtonStateService.ResolveObsTest(
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

    private static void ApplyButtonState(System.Windows.Controls.Button button, ConnectionButtonState state)
    {
        button.IsEnabled = state.IsEnabled;
        ButtonIconContentService.SetButtonIcon(button, state.Content, state.IconKey);
    }

    private void UpdateTwitchLiveIndicator()
    {
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        if (_streamStatus is { IsLive: true })
        {
            var liveBrush = FrozenBrushFrom("#FF2D55");
            TwitchLiveDot.Fill = liveBrush;
            TwitchLiveDot.Stroke = liveBrush;
            TwitchLiveStateText.Text = _text.Get(UiTextKeys.TwitchLive);
            TwitchLiveStateText.Foreground = liveBrush;
            TopProfileText.Text = _text.Get(UiTextKeys.TwitchProfile);
            TopProfileText.Foreground = palette.Text;
            return;
        }

        TwitchLiveDot.Fill = System.Windows.Media.Brushes.Transparent;
        TwitchLiveDot.Stroke = palette.SidebarText;
        TwitchLiveStateText.Text = _text.Get(UiTextKeys.TwitchOffline);
        TwitchLiveStateText.Foreground = palette.SidebarText;
        TopProfileText.Text = _text.Get(UiTextKeys.TwitchProfile);
        TopProfileText.Foreground = palette.Text;
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
        var brightnessPercent = CircularProgressGeometryService.ToPercent(BrightnessSlider.Value, BrightnessSlider.Maximum);
        var backgroundBrightnessPercent = CircularProgressGeometryService.ToPercent(BackgroundBrightnessSlider.Value, BackgroundBrightnessSlider.Maximum);

        _updatingLightValueFields = true;
        try
        {
            BrightnessValueText.Text = $"{brightnessPercent}%";
            DurationValueText.Text = ((int)Math.Round(DurationSlider.Value)).ToString();
            CycleValueText.Text = ((int)Math.Round(CycleSlider.Value)).ToString();
            StepValueText.Text = ((int)Math.Round(StepSlider.Value)).ToString();
            BackgroundBrightnessValueText.Text = $"{backgroundBrightnessPercent}%";
            BackgroundCycleValueText.Text = ((int)Math.Round(BackgroundCycleSlider.Value)).ToString();
            BackgroundStepValueText.Text = ((int)Math.Round(BackgroundStepSlider.Value)).ToString();
            AlertVolumeValueText.Text = $"{(int)Math.Round(AlertVolumeSlider.Value)}%";
        }
        finally
        {
            _updatingLightValueFields = false;
        }

        UpdateCircularProgress(BrightnessArc, brightnessPercent / 100d);
        UpdateCircularProgress(BackgroundBrightnessArc, backgroundBrightnessPercent / 100d);
    }

    private static void UpdateCircularProgress(System.Windows.Shapes.Path path, double progress)
    {
        path.Data = CircularProgressGeometryService.BuildArcGeometry(progress);
    }

}
