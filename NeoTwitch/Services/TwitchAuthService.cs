using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using NeoTwitch.Models;

namespace NeoTwitch.Services;

public sealed class TwitchAuthService
{
    public static readonly string[] RequiredScopes =
    [
        "moderator:read:followers",
        "channel:read:subscriptions",
        "channel:read:redemptions",
        "bits:read",
        "user:read:chat",
        "user:write:chat"
    ];

    private readonly HttpClient _http = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<DeviceCodeSession> BeginDeviceFlowAsync(string clientId, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["scopes"] = string.Join(' ', RequiredScopes)
        });

        using var response = await _http.PostAsync("https://id.twitch.tv/oauth2/device", content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Twitch no inicio el login: {json}");
        }

        var result = JsonSerializer.Deserialize<DeviceCodeResponse>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Twitch envio una respuesta de login vacia.");

        return new DeviceCodeSession(
            result.DeviceCode,
            result.UserCode,
            result.VerificationUri,
            result.ExpiresIn,
            Math.Max(result.Interval, 5));
    }

    public void OpenVerificationPage(DeviceCodeSession session)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = session.VerificationUri,
            UseShellExecute = true
        });
    }

    public async Task<TwitchTokenInfo> PollForTokenAsync(string clientId, DeviceCodeSession session, Action<string> log, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(session.ExpiresIn);

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(session.IntervalSeconds), cancellationToken);

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["device_code"] = session.DeviceCode,
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
            });

            using var response = await _http.PostAsync("https://id.twitch.tv/oauth2/token", content, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var token = JsonSerializer.Deserialize<TokenResponse>(json, _jsonOptions)
                    ?? throw new InvalidOperationException("Twitch envio un token vacio.");

                return ToTokenInfo(token);
            }

            var oauthError = TryReadError(json);
            if (oauthError is "authorization_pending")
            {
                log("Esperando autorizacion en Twitch...");
                continue;
            }

            if (oauthError is "slow_down")
            {
                log("Twitch pidio bajar la frecuencia de consulta.");
                await Task.Delay(TimeSpan.FromSeconds(session.IntervalSeconds + 5), cancellationToken);
                continue;
            }

            throw new InvalidOperationException($"Twitch rechazo el login: {json}");
        }

        throw new TimeoutException("El codigo de Twitch expiro antes de autorizar la app.");
    }

    public async Task EnsureValidTokenAsync(AppConfig config, Action<string> log, CancellationToken cancellationToken)
    {
        if (!config.Token.NeedsRefresh)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(config.Token.RefreshToken))
        {
            throw new InvalidOperationException("Twitch necesita iniciar sesion otra vez.");
        }

        var fields = new Dictionary<string, string>
        {
            ["client_id"] = config.TwitchClientId,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = config.Token.RefreshToken
        };

        if (!string.IsNullOrWhiteSpace(config.TwitchClientSecret))
        {
            fields["client_secret"] = config.TwitchClientSecret.Trim();
        }

        using var content = new FormUrlEncodedContent(fields);

        using var response = await _http.PostAsync("https://id.twitch.tv/oauth2/token", content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"No pude refrescar Twitch: {json}");
        }

        var token = JsonSerializer.Deserialize<TokenResponse>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Twitch envio un refresh vacio.");

        config.Token = ToTokenInfo(token);
        log("Token de Twitch actualizado.");
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
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitch.tv/helix/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Token.AccessToken);
        request.Headers.Add("Client-Id", config.TwitchClientId);

        using var response = await _http.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"No pude leer el canal de Twitch: {json}");
        }

        using var doc = JsonDocument.Parse(json);
        var first = doc.RootElement.GetProperty("data").EnumerateArray().FirstOrDefault();
        if (first.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException("Twitch no envio datos del usuario.");
        }

        return new TwitchChannelInfo
        {
            UserId = first.GetProperty("id").GetString() ?? "",
            Login = first.GetProperty("login").GetString() ?? "",
            DisplayName = first.GetProperty("display_name").GetString() ?? "",
            ProfileImageUrl = first.TryGetProperty("profile_image_url", out var profileImageUrl)
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

        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.twitch.tv/helix/streams?user_id={Uri.EscapeDataString(config.Channel.UserId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Token.AccessToken);
        request.Headers.Add("Client-Id", config.TwitchClientId);

        using var response = await _http.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"No pude leer el directo de Twitch: {json}");
        }

        using var doc = JsonDocument.Parse(json);
        var first = doc.RootElement.GetProperty("data").EnumerateArray().FirstOrDefault();
        if (first.ValueKind == JsonValueKind.Undefined)
        {
            return TwitchStreamStatus.Offline;
        }

        return new TwitchStreamStatus(
            true,
            first.TryGetProperty("viewer_count", out var viewerCount) ? viewerCount.GetInt32() : 0,
            first.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
            first.TryGetProperty("game_name", out var gameName) ? gameName.GetString() ?? "" : "");
    }

    private static TwitchTokenInfo ToTokenInfo(TokenResponse token)
    {
        return new TwitchTokenInfo
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(token.ExpiresIn, 60)),
            Scopes = token.Scope ?? []
        };
    }

    private static string? TryReadError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("error", out var error) ? error.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    private sealed class DeviceCodeResponse
    {
        [JsonPropertyName("device_code")]
        public string DeviceCode { get; set; } = "";

        [JsonPropertyName("user_code")]
        public string UserCode { get; set; } = "";

        [JsonPropertyName("verification_uri")]
        public string VerificationUri { get; set; } = "";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("interval")]
        public int Interval { get; set; }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = "";

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = "";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
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
