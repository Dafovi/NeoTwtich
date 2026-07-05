using System.Windows;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;
using static NeoTwitch.Services.Text.UiTextFormatter;

namespace NeoTwitch;

public partial class MainWindow
{
    private void OpenTwitchConsole()
    {
        _externalLauncher.Open(NeoTwitchProduct.Links.TwitchDeveloperApps);
        AddLog(_text.Get(Services.Text.UiTextKeys.ExternalTwitchConsoleOpenedLog), ActivityLogKind.Twitch);
    }

    private void OpenTwitchProfileButton_Click(object sender, RoutedEventArgs e)
    {
        var channel = FirstNonEmpty(_config.Channel.Login, _config.Channel.DisplayName)
            .Trim()
            .TrimStart('@');

        if (string.IsNullOrWhiteSpace(channel))
        {
            _dialog.ShowInformation(_text.Get(Services.Text.UiTextKeys.TwitchTitle), _text.Get(Services.Text.UiTextKeys.ExternalTwitchConnectFirstPrompt));
            return;
        }

        _externalLauncher.Open(NeoTwitchProduct.Links.TwitchChannel(channel));
        AddLog(_text.Format(Services.Text.UiTextKeys.ExternalTwitchProfileOpeningLog, channel), ActivityLogKind.Twitch);
    }

    private void OpenAlexaConsole()
    {
        _externalLauncher.Open(NeoTwitchProduct.Links.AlexaDeveloperConsole);
        AddLog(_text.Get(Services.Text.UiTextKeys.ExternalAlexaConsoleOpenedLog), ActivityLogKind.Alexa);
    }

    private void OpenArduinoSketch()
    {
        _externalLauncher.Open(NeoTwitchProduct.Links.ArduinoSketch);
        AddLog(_text.Get(Services.Text.UiTextKeys.ExternalArduinoSketchOpenedLog), ActivityLogKind.Arduino);
    }

    private void OpenArduinoGuide()
    {
        _externalLauncher.Open(NeoTwitchProduct.Links.ArduinoGuide);
        AddLog(_text.Get(Services.Text.UiTextKeys.ExternalArduinoGuideOpenedLog), ActivityLogKind.Arduino);
    }
}
