using System.Windows;
using System.Windows.Controls;
using static NeoTwitch.Services.Text.UiTextFormatter;

namespace NeoTwitch;

public partial class MainWindow
{
    private void ToggleClientIdVisibility()
    {
        _showClientId = !_showClientId;
        UpdateSensitiveFieldVisibility();
    }

    private void ToggleClientSecretVisibility()
    {
        _showClientSecret = !_showClientSecret;
        UpdateSensitiveFieldVisibility();
    }

    private void ToggleAlexaRelayUrlVisibility()
    {
        _showAlexaRelayUrl = !_showAlexaRelayUrl;
        UpdateSensitiveFieldVisibility();
    }

    private void ToggleAlexaAuthTokenVisibility()
    {
        _showAlexaAuthToken = !_showAlexaAuthToken;
        UpdateSensitiveFieldVisibility();
    }

    private void ToggleObsPasswordVisibility()
    {
        _showObsPassword = !_showObsPassword;
        UpdateSensitiveFieldVisibility();
    }

    private void UpdateSensitiveFieldVisibility()
    {
        if (_initializingComponent)
        {
            return;
        }

        UpdateSensitiveField(ClientIdBox, ClientIdMaskText, ClientIdRevealButton, _showClientId);
        UpdateSensitiveField(ClientSecretBox, ClientSecretMaskText, ClientSecretRevealButton, _showClientSecret);
        UpdateSensitiveField(AlexaRelayUrlBox, AlexaRelayUrlMaskText, AlexaRelayUrlRevealButton, _showAlexaRelayUrl);
        UpdateSensitiveField(AlexaAuthTokenBox, AlexaAuthTokenMaskText, AlexaAuthTokenRevealButton, _showAlexaAuthToken);
        UpdateSensitiveField(ObsPasswordBox, ObsPasswordMaskText, ObsPasswordRevealButton, _showObsPassword);
    }

    private static void UpdateSensitiveField(
        System.Windows.Controls.TextBox textBox,
        TextBlock maskText,
        System.Windows.Controls.Button revealButton,
        bool isVisible)
    {
        var shouldMask = !isVisible && !string.IsNullOrWhiteSpace(textBox.Text);
        textBox.IsHitTestVisible = !shouldMask;
        maskText.Visibility = shouldMask ? Visibility.Visible : Visibility.Collapsed;
        maskText.Text = shouldMask ? BuildSecretMask(textBox.Text) : "";
        revealButton.Content = isVisible ? "Ocultar" : "Ver";
    }
}
