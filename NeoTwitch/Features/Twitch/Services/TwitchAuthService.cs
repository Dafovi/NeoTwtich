using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using NeoTwitch.Models;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using Protocol = NeoTwitch.Services.TwitchAuthProtocol;

namespace NeoTwitch.Services;

public sealed class TwitchAuthService
{
    public static readonly string[] RequiredScopes = Protocol.RequiredScopes;

    private readonly HttpClient _http;
    private readonly IExternalLauncherService _externalLauncher;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IUiTextService _text;
    private readonly TimeProvider _timeProvider;
    private readonly object _refreshSync = new();
    private Task? _activeRefresh;
    private AppConfig? _activeRefreshConfig;

    public TwitchAuthService(
        IUiTextService text,
        IExternalLauncherService externalLauncher,
        TimeProvider timeProvider,
        HttpClient? httpClient = null)
    {
        _text = text;
        _externalLauncher = externalLauncher;
        _timeProvider = timeProvider;
        _http = httpClient ?? new HttpClient();
    }

    public async Task<DeviceCodeSession> BeginDeviceFlowAsync(string clientId, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            [Protocol.FormFields.ClientId] = clientId,
            [Protocol.FormFields.Scopes] = string.Join(' ', RequiredScopes)
        });

        using var response = await _http.PostAsync(Protocol.DeviceCodeUrl, content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(_text.Format(UiTextKeys.TwitchAuthLoginStartFailure, json));
        }

        var result = JsonSerializer.Deserialize<DeviceCodeResponse>(json, _jsonOptions)
            ?? throw new InvalidOperationException(_text.Get(UiTextKeys.TwitchAuthEmptyLoginResponse));

        return new DeviceCodeSession(
            result.DeviceCode,
            result.UserCode,
            result.VerificationUri,
            result.ExpiresIn,
            Math.Max(result.Interval, 5));
    }

    public void OpenVerificationPage(DeviceCodeSession session)
    {
        _externalLauncher.Open(session.VerificationUri);
    }

    public async Task<TwitchTokenInfo> PollForTokenAsync(string clientId, DeviceCodeSession session, Action<string> log, CancellationToken cancellationToken)
    {
        var deadline = _timeProvider.GetUtcNow().AddSeconds(session.ExpiresIn);

        while (_timeProvider.GetUtcNow() < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(session.IntervalSeconds), cancellationToken);

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [Protocol.FormFields.ClientId] = clientId,
                [Protocol.FormFields.DeviceCode] = session.DeviceCode,
                [Protocol.FormFields.GrantType] = Protocol.GrantTypes.DeviceCode
            });

            using var response = await _http.PostAsync(Protocol.TokenUrl, content, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var token = JsonSerializer.Deserialize<TokenResponse>(json, _jsonOptions)
                    ?? throw new InvalidOperationException(_text.Get(UiTextKeys.TwitchAuthEmptyTokenResponse));

                return ToTokenInfo(token);
            }

            var oauthError = TryReadError(json);
            if (oauthError is Protocol.OAuthErrors.AuthorizationPending)
            {
                log(_text.Get(UiTextKeys.TwitchAuthAuthorizationPendingLog));
                continue;
            }

            if (oauthError is Protocol.OAuthErrors.SlowDown)
            {
                log(_text.Get(UiTextKeys.TwitchAuthSlowDownLog));
                await Task.Delay(TimeSpan.FromSeconds(session.IntervalSeconds + 5), cancellationToken);
                continue;
            }

            throw new InvalidOperationException(_text.Format(UiTextKeys.TwitchAuthLoginRejected, json));
        }

        throw new TimeoutException(_text.Get(UiTextKeys.TwitchAuthDeviceCodeExpired));
    }

    public async Task EnsureValidTokenAsync(AppConfig config, Action<string> log, CancellationToken cancellationToken)
    {
        if (!TwitchTokenRefreshPolicy.NeedsRefresh(config.Token, _timeProvider.GetUtcNow()))
        {
            return;
        }

        while (true)
        {
            Task refreshTask;
            AppConfig refreshConfig;
            lock (_refreshSync)
            {
                if (_activeRefresh is null || _activeRefresh.IsCompleted)
                {
                    _activeRefreshConfig = config;
                    _activeRefresh = RefreshTokenCoreAsync(config, log);
                }

                refreshTask = _activeRefresh;
                refreshConfig = _activeRefreshConfig!;
            }

            // Caller cancellation stops only this wait; it does not cancel the shared HTTP refresh
            // that other callers depend on.
            await refreshTask.WaitAsync(cancellationToken);

            if (ReferenceEquals(refreshConfig, config)
                || !TwitchTokenRefreshPolicy.NeedsRefresh(config.Token, _timeProvider.GetUtcNow()))
            {
                return;
            }
        }
    }

    private async Task RefreshTokenCoreAsync(AppConfig config, Action<string> log)
    {
        var tokenBeingRefreshed = config.Token;
        if (string.IsNullOrWhiteSpace(tokenBeingRefreshed.RefreshToken))
        {
            throw new InvalidOperationException(_text.Get(UiTextKeys.TwitchAuthLoginRequired));
        }

        log(_text.Get(UiTextKeys.TwitchAuthTokenRefreshStartedLog));
        var fields = new Dictionary<string, string>
        {
            [Protocol.FormFields.ClientId] = config.TwitchClientId,
            [Protocol.FormFields.GrantType] = Protocol.GrantTypes.RefreshToken,
            [Protocol.FormFields.RefreshToken] = tokenBeingRefreshed.RefreshToken
        };

        if (!string.IsNullOrWhiteSpace(config.TwitchClientSecret))
        {
            fields[Protocol.FormFields.ClientSecret] = config.TwitchClientSecret.Trim();
        }

        using var content = new FormUrlEncodedContent(fields);
        using var response = await _http.PostAsync(Protocol.TokenUrl, content, CancellationToken.None);
        var json = await response.Content.ReadAsStringAsync(CancellationToken.None);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(_text.Format(UiTextKeys.TwitchAuthRefreshFailure, json));
        }

        var token = JsonSerializer.Deserialize<TokenResponse>(json, _jsonOptions)
            ?? throw new InvalidOperationException(_text.Get(UiTextKeys.TwitchAuthEmptyRefreshResponse));
        var refreshedToken = ToTokenInfo(token);

        if (!ReferenceEquals(config.Token, tokenBeingRefreshed))
        {
            log(_text.Get(UiTextKeys.TwitchAuthStaleRefreshDiscardedLog));
            return;
        }

        config.Token = refreshedToken;
        log(_text.Get(UiTextKeys.TwitchAuthTokenRefreshedLog));
    }

    public static IReadOnlyList<string> GetMissingScopes(TwitchTokenInfo token)
    {
        var grantedScopes = token.Scopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return RequiredScopes
            .Where(scope => !grantedScopes.Contains(scope))
            .ToArray();
    }

    public async Task<TwitchChannelInfo> GetCurrentUserAsync(AppConfig config, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Protocol.UsersUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue(Protocol.BearerScheme, config.Token.AccessToken);
        request.Headers.Add(Protocol.ClientIdHeader, config.TwitchClientId);

        using var response = await _http.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(_text.Format(UiTextKeys.TwitchAuthReadChannelFailure, json));
        }

        using var doc = JsonDocument.Parse(json);
        var first = doc.RootElement.GetProperty(Protocol.Json.Data).EnumerateArray().FirstOrDefault();
        if (first.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException(_text.Get(UiTextKeys.TwitchAuthMissingUserData));
        }

        return new TwitchChannelInfo
        {
            UserId = first.GetProperty(Protocol.Json.Id).GetString() ?? "",
            Login = first.GetProperty(Protocol.Json.Login).GetString() ?? "",
            DisplayName = first.GetProperty(Protocol.Json.DisplayName).GetString() ?? "",
            ProfileImageUrl = first.TryGetProperty(Protocol.Json.ProfileImageUrl, out var profileImageUrl)
                ? profileImageUrl.GetString() ?? ""
                : ""
        };
    }

    public async Task<TwitchStreamStatus> GetStreamStatusAsync(AppConfig config, CancellationToken cancellationToken)
    {
        if (!config.Channel.IsReady)
        {
            return TwitchStreamStatus.Offline;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, Protocol.BuildStreamsUrl(config.Channel.UserId));
        request.Headers.Authorization = new AuthenticationHeaderValue(Protocol.BearerScheme, config.Token.AccessToken);
        request.Headers.Add(Protocol.ClientIdHeader, config.TwitchClientId);

        using var response = await _http.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(_text.Format(UiTextKeys.TwitchAuthReadStreamFailure, json));
        }

        using var doc = JsonDocument.Parse(json);
        var first = doc.RootElement.GetProperty(Protocol.Json.Data).EnumerateArray().FirstOrDefault();
        if (first.ValueKind == JsonValueKind.Undefined)
        {
            return TwitchStreamStatus.Offline;
        }

        return new TwitchStreamStatus(
            true,
            first.TryGetProperty(Protocol.Json.ViewerCount, out var viewerCount) ? viewerCount.GetInt32() : 0,
            first.TryGetProperty(Protocol.Json.Title, out var title) ? title.GetString() ?? "" : "",
            first.TryGetProperty(Protocol.Json.GameName, out var gameName) ? gameName.GetString() ?? "" : "");
    }

    private TwitchTokenInfo ToTokenInfo(TokenResponse token)
    {
        return new TwitchTokenInfo
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            ExpiresAt = _timeProvider.GetUtcNow().AddSeconds(Math.Max(token.ExpiresIn, 60)),
            Scopes = token.Scope ?? []
        };
    }

    private static string? TryReadError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(Protocol.Json.Error, out var error) ? error.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    private sealed class DeviceCodeResponse
    {
        [JsonPropertyName(Protocol.Json.DeviceCode)]
        public string DeviceCode { get; set; } = "";

        [JsonPropertyName(Protocol.Json.UserCode)]
        public string UserCode { get; set; } = "";

        [JsonPropertyName(Protocol.Json.VerificationUri)]
        public string VerificationUri { get; set; } = "";

        [JsonPropertyName(Protocol.Json.ExpiresIn)]
        public int ExpiresIn { get; set; }

        [JsonPropertyName(Protocol.Json.Interval)]
        public int Interval { get; set; }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName(Protocol.Json.AccessToken)]
        public string AccessToken { get; set; } = "";

        [JsonPropertyName(Protocol.Json.RefreshToken)]
        public string RefreshToken { get; set; } = "";

        [JsonPropertyName(Protocol.Json.ExpiresIn)]
        public int ExpiresIn { get; set; }

        [JsonPropertyName(Protocol.Json.Scope)]
        public string[]? Scope { get; set; }
    }
}

public sealed record DeviceCodeSession(
    string DeviceCode,
    string UserCode,
    string VerificationUri,
    int ExpiresIn,
    int IntervalSeconds);

public sealed record TwitchStreamStatus(
    bool IsLive,
    int ViewerCount,
    string Title,
    string GameName)
{
    public static TwitchStreamStatus Offline { get; } = new(false, 0, "", "");
}
