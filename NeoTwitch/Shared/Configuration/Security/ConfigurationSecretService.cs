using NeoTwitch.Models;

namespace NeoTwitch.Services.Configuration.Security;

public sealed record ConfigurationSecretLoadResult(
    bool HadLegacyPlaintext,
    IReadOnlyList<string> FailedPurposes);

public sealed class ConfigurationSecretService
{
    private const string TwitchClientSecretPurpose = "twitch-client-secret";
    private const string TwitchAccessTokenPurpose = "twitch-access-token";
    private const string TwitchRefreshTokenPurpose = "twitch-refresh-token";
    private const string AlexaAuthTokenPurpose = "alexa-auth-token";
    private const string ObsPasswordPurpose = "obs-password";

    private readonly IConfigurationSecretProtector _protector;

    public ConfigurationSecretService(IConfigurationSecretProtector protector)
    {
        _protector = protector;
    }

    public bool HasPlaintextSecrets(AppConfig config) =>
        !string.IsNullOrEmpty(config.TwitchClientSecret)
        || !string.IsNullOrEmpty(config.Token.AccessToken)
        || !string.IsNullOrEmpty(config.Token.RefreshToken)
        || !string.IsNullOrEmpty(config.Alexa.AuthToken)
        || !string.IsNullOrEmpty(config.Obs.Password);

    public bool HasReplacementsFor(AppConfig config, IEnumerable<string> failedPurposes)
    {
        return failedPurposes.All(purpose => purpose switch
        {
            TwitchClientSecretPurpose => !string.IsNullOrWhiteSpace(config.TwitchClientSecret),
            TwitchAccessTokenPurpose => !string.IsNullOrWhiteSpace(config.Token.AccessToken),
            TwitchRefreshTokenPurpose => !string.IsNullOrWhiteSpace(config.Token.RefreshToken),
            AlexaAuthTokenPurpose => !string.IsNullOrWhiteSpace(config.Alexa.AuthToken),
            ObsPasswordPurpose => !string.IsNullOrWhiteSpace(config.Obs.Password),
            _ => false
        });
    }

    public void ProtectForPersistence(AppConfig config)
    {
        config.ProtectedSecrets = new ProtectedConfigurationSecrets
        {
            TwitchClientSecret = _protector.Protect(TwitchClientSecretPurpose, config.TwitchClientSecret),
            TwitchAccessToken = _protector.Protect(TwitchAccessTokenPurpose, config.Token.AccessToken),
            TwitchRefreshToken = _protector.Protect(TwitchRefreshTokenPurpose, config.Token.RefreshToken),
            AlexaAuthToken = _protector.Protect(AlexaAuthTokenPurpose, config.Alexa.AuthToken),
            ObsPassword = _protector.Protect(ObsPasswordPurpose, config.Obs.Password)
        };

        ClearPlaintext(config);
    }

    public ConfigurationSecretLoadResult RestoreForRuntime(AppConfig config)
    {
        var hadLegacyPlaintext = HasPlaintextSecrets(config);
        var failures = new List<string>();
        config.ProtectedSecrets ??= new ProtectedConfigurationSecrets();

        RestoreOne(
            TwitchClientSecretPurpose,
            config.ProtectedSecrets.TwitchClientSecret,
            config.TwitchClientSecret,
            value => config.TwitchClientSecret = value,
            failures);
        RestoreOne(
            TwitchAccessTokenPurpose,
            config.ProtectedSecrets.TwitchAccessToken,
            config.Token.AccessToken,
            value => config.Token.AccessToken = value,
            failures);
        RestoreOne(
            TwitchRefreshTokenPurpose,
            config.ProtectedSecrets.TwitchRefreshToken,
            config.Token.RefreshToken,
            value => config.Token.RefreshToken = value,
            failures);
        RestoreOne(
            AlexaAuthTokenPurpose,
            config.ProtectedSecrets.AlexaAuthToken,
            config.Alexa.AuthToken,
            value => config.Alexa.AuthToken = value,
            failures);
        RestoreOne(
            ObsPasswordPurpose,
            config.ProtectedSecrets.ObsPassword,
            config.Obs.Password,
            value => config.Obs.Password = value,
            failures);

        return new ConfigurationSecretLoadResult(hadLegacyPlaintext, failures);
    }

    public static void RemoveFromExport(AppConfig config)
    {
        ClearPlaintext(config);
        config.ProtectedSecrets = new ProtectedConfigurationSecrets();
    }

    private void RestoreOne(
        string purpose,
        string protectedValue,
        string legacyPlaintext,
        Action<string> assign,
        ICollection<string> failures)
    {
        if (!string.IsNullOrEmpty(protectedValue))
        {
            try
            {
                assign(_protector.Unprotect(purpose, protectedValue));
            }
            catch
            {
                assign("");
                failures.Add(purpose);
            }

            return;
        }

        assign(legacyPlaintext ?? "");
    }

    private static void ClearPlaintext(AppConfig config)
    {
        config.TwitchClientSecret = "";
        config.Token.AccessToken = "";
        config.Token.RefreshToken = "";
        config.Alexa.AuthToken = "";
        config.Obs.Password = "";
    }
}
