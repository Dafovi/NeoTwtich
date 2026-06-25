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
using NeoTwitch.Services;
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
        var twitchBusy = _isTwitchAuthorizing || _isTwitchConnecting;
        TwitchButton.IsEnabled = !twitchBusy;
        TwitchButton.Content = _isTwitchAuthorizing
            ? "Autorizando..."
            : _isTwitchConnecting
                ? "Conectando..."
                : _eventSubClient.IsRunning
                    ? "Desconectar Twitch"
                    : "Conectar Twitch";

        ConnectArduinoButton.IsEnabled = !_isArduinoConnecting && _config.ArduinoEnabled;
        ConnectArduinoButton.Content = _isArduinoConnecting
            ? "Conectando..."
            : "Conectar Arduino";

        TestAlexaButton.IsEnabled = !_isAlexaConnecting && _config.Alexa.Enabled;
        TestAlexaButton.Content = _isAlexaConnecting
            ? "Probando..."
            : "Probar Alexa";

        var obsBusy = _isObsConnecting || _isObsSceneActionRunning;
        ConnectObsButton.IsEnabled = !obsBusy && _config.Obs.Enabled;
        ConnectObsButton.Content = _isObsConnecting
            ? "Conectando..."
            : _obsService.IsConnected
                ? "Desconectar OBS"
                : "Conectar OBS";
        TestObsButton.IsEnabled = !obsBusy && _config.Obs.Enabled;
        TestObsButton.Content = _isObsConnecting
            ? "Actualizando..."
            : "Actualizar escenas";
    }

    private string BuildAlexaSidebarStatusText()
    {
        var background = _config.BackgroundAlexaEnabled
            ? $"Fondo: {_config.BackgroundAlexaOnEventName}"
            : "Fondo sin mantener";

        var endBehavior = _config.BackgroundAlexaTurnOffAfterEvent
            ? $"Al finalizar: {_config.BackgroundAlexaOffEventName}"
            : "Al finalizar: conserva estado";

        return $"{background}. {endBehavior}.";
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

    private void ApplyWindowChromeColor()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            var captionColor = AppCaptionColor;
            var borderColor = AppCaptionColor;
            var textColor = AppCaptionTextColor;
            var size = Marshal.SizeOf<int>();
            _ = DwmSetWindowAttribute(hwnd, DwmWindowAttributeCaptionColor, ref captionColor, size);
            _ = DwmSetWindowAttribute(hwnd, DwmWindowAttributeBorderColor, ref borderColor, size);
            _ = DwmSetWindowAttribute(hwnd, DwmWindowAttributeTextColor, ref textColor, size);
        }
        catch
        {
            // Older Windows builds ignore custom title bar colors.
        }
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

    private void Window_StateChanged(object? sender, EventArgs e)
    {
    }

    private void CustomTitleDragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsInsideButton(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (e.ClickCount >= 2)
        {
            ToggleWindowState();
            return;
        }

        if (_isCustomMaximized)
        {
            RestoreWindowFromWorkArea();
        }

        try
        {
            DragMove();
        }
        catch
        {
            // DragMove can throw if the pointer is released before the drag starts.
        }
    }

    private void MinimizeWindowButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeWindowButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleWindowState();
    }

    private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleWindowState()
    {
        if (_isCustomMaximized)
        {
            RestoreWindowFromWorkArea();
            return;
        }

        MaximizeWindowToWorkArea();
    }

    private void MaximizeWindowToWorkArea()
    {
        _restoreWindowBounds = new Rect(Left, Top, Width, Height);

        var handle = new WindowInteropHelper(this).Handle;
        var area = Forms.Screen.FromHandle(handle).WorkingArea;
        var source = PresentationSource.FromVisual(this);
        var transform = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var topLeft = transform.Transform(new System.Windows.Point(area.Left, area.Top));
        var bottomRight = transform.Transform(new System.Windows.Point(area.Right, area.Bottom));

        WindowState = WindowState.Normal;
        Left = topLeft.X;
        Top = topLeft.Y;
        Width = Math.Max(MinWidth, bottomRight.X - topLeft.X);
        Height = Math.Max(MinHeight, bottomRight.Y - topLeft.Y);
        _isCustomMaximized = true;
    }

    private void RestoreWindowFromWorkArea()
    {
        WindowState = WindowState.Normal;
        _isCustomMaximized = false;

        if (_restoreWindowBounds.IsEmpty)
        {
            return;
        }

        Left = _restoreWindowBounds.Left;
        Top = _restoreWindowBounds.Top;
        Width = _restoreWindowBounds.Width;
        Height = _restoreWindowBounds.Height;
    }

    private static bool IsInsideButton(DependencyObject? source)
    {
        for (var current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is System.Windows.Controls.Button)
            {
                return true;
            }
        }

        return false;
    }
}
