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
        ApplyButtonState(TwitchButton, ConnectionButtonStateService.ResolveTwitch(
            _isTwitchAuthorizing,
            _isTwitchConnecting,
            _eventSubClient.IsRunning));
        ApplyButtonState(ConnectArduinoButton, ConnectionButtonStateService.ResolveArduino(
            _config.ArduinoEnabled,
            _isArduinoConnecting));
        ApplyButtonState(TestAlexaButton, ConnectionButtonStateService.ResolveAlexa(
            _config.Alexa.Enabled,
            _isAlexaConnecting));
        ApplyButtonState(ConnectObsButton, ConnectionButtonStateService.ResolveObs(
            _config.Obs.Enabled,
            _isObsConnecting,
            _isObsSceneActionRunning,
            _obsService.IsConnected));
        ApplyButtonState(TestObsButton, ConnectionButtonStateService.ResolveObsTest(
            _config.Obs.Enabled,
            _isObsConnecting,
            _isObsSceneActionRunning));
    }

    private static void ApplyButtonState(System.Windows.Controls.Button button, ConnectionButtonState state)
    {
        button.IsEnabled = state.IsEnabled;
        button.Content = state.Content;
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
            TwitchLiveStateText.Text = "En directo";
            TwitchLiveStateText.Foreground = liveBrush;
            TopProfileText.Text = "Perfil";
            TopProfileText.Foreground = palette.Text;
            return;
        }

        TwitchLiveDot.Fill = System.Windows.Media.Brushes.Transparent;
        TwitchLiveDot.Stroke = palette.SidebarText;
        TwitchLiveStateText.Text = "No esta en directo";
        TwitchLiveStateText.Foreground = palette.SidebarText;
        TopProfileText.Text = "Perfil";
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
