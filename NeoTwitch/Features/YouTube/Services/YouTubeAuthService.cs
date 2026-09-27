using System.Net.Http;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NeoTwitch.Models;
using NeoTwitch.Services.Ui;

namespace NeoTwitch.Services.YouTube;

/// <summary>
/// OAuth 2.0 authorization-code flow with PKCE for the desktop client.
/// </summary>
public sealed class YouTubeAuthService : IDisposable
{
    private const int MinimumVerifierLength = 43;
    private readonly HttpClient _http;
    private readonly IExternalLauncherService _externalLauncher;
    private readonly TimeProvider _timeProvider;
    private readonly bool _ownsHttpClient;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private int _disposed;

    public YouTubeAuthService(
        IExternalLauncherService externalLauncher,
        TimeProvider timeProvider,
        HttpClient? httpClient = null)
    {
        _externalLauncher = externalLauncher;
        _timeProvider = timeProvider;
        _http = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient is null;
    }

    public YouTubeAuthorizationRequest BeginAuthorization(string clientId, Uri redirectUri)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("Configura el Client ID de YouTube antes de conectar la cuenta.");
        }

        ArgumentNullException.ThrowIfNull(redirectUri);
        if (!string.Equals(redirectUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || !IsLoopbackHost(redirectUri.Host))
        {
            throw new ArgumentException("El retorno OAuth de YouTube debe usar una dirección HTTP local.", nameof(redirectUri));
        }

        var verifier = CreateCodeVerifier();
        var state = CreateUrlSafeRandom(32);
        var challenge = CreateCodeChallenge(verifier);
        var query = new Dictionary<string, string>
        {
            ["client_id"] = clientId.Trim(),
            ["redirect_uri"] = redirectUri.AbsoluteUri,
            ["response_type"] = "code",
            ["scope"] = string.Join(' ', YouTubeOAuthProtocol.RequiredScopes),
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state,
            ["access_type"] = "offline",
            // Google otherwise may omit a refresh token when a user previously approved the client.
            ["prompt"] = "consent"
        };

        var authorizationUri = new UriBuilder(YouTubeOAuthProtocol.AuthorizationUrl)
        {
            Query = string.Join('&', query.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"))
        }.Uri;

        return new YouTubeAuthorizationRequest(authorizationUri, redirectUri, state, verifier);
    }

    public void OpenAuthorizationPage(YouTubeAuthorizationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _externalLauncher.Open(request.AuthorizationUri.AbsoluteUri);
    }

    public async Task<YouTubeTokenInfo> ExchangeAuthorizationCodeAsync(
        string clientId,
        string clientSecret,
        YouTubeAuthorizationRequest request,
        string authorizationCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(authorizationCode))
        {
            throw new InvalidOperationException("YouTube no devolvió un código de autorización.");
        }

        var fields = new Dictionary<string, string>
        {
            ["client_id"] = clientId.Trim(),
            ["code"] = authorizationCode,
            ["code_verifier"] = request.CodeVerifier,
            ["redirect_uri"] = request.RedirectUri.AbsoluteUri,
            ["grant_type"] = "authorization_code"
        };
        AddClientSecret(fields, clientSecret);

        return await RequestTokenAsync(fields, cancellationToken);
    }

    public async Task EnsureValidTokenAsync(AppConfig config, CancellationToken cancellationToken)
    {
        if (!YouTubeTokenRefreshPolicy.NeedsRefresh(config.YouTubeToken, _timeProvider.GetUtcNow()))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(config.YouTubeToken.RefreshToken))
        {
            throw new InvalidOperationException("La cuenta de YouTube necesita autorizarse de nuevo.");
        }

        var oldToken = config.YouTubeToken;
        var fields = new Dictionary<string, string>
        {
            ["client_id"] = config.YouTubeClientId.Trim(),
            ["refresh_token"] = oldToken.RefreshToken,
            ["grant_type"] = "refresh_token"
        };
        AddClientSecret(fields, config.YouTubeClientSecret);
        var refreshed = await RequestTokenAsync(fields, cancellationToken);
        if (string.IsNullOrWhiteSpace(refreshed.RefreshToken))
        {
            refreshed.RefreshToken = oldToken.RefreshToken;
        }

        if (ReferenceEquals(config.YouTubeToken, oldToken))
        {
            config.YouTubeToken = refreshed;
        }
    }

    public static string CreateCodeChallenge(string verifier)
    {
        if (string.IsNullOrWhiteSpace(verifier) || verifier.Length < MinimumVerifierLength)
        {
            throw new ArgumentException("El verificador PKCE no tiene una longitud válida.", nameof(verifier));
        }

        return Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
    }

    private async Task<YouTubeTokenInfo> RequestTokenAsync(
        IReadOnlyDictionary<string, string> fields,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(fields);
        using var response = await _http.PostAsync(YouTubeOAuthProtocol.TokenUrl, content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"No fue posible autorizar YouTube (HTTP {(int)response.StatusCode}; {DescribeOAuthError(json)}).\n" +
                "No se mostró ningún token ni dato privado.");
        }

        var responseModel = JsonSerializer.Deserialize<TokenResponse>(json, _jsonOptions)
            ?? throw new InvalidOperationException("YouTube devolvió una respuesta de autorización vacía.");
        if (string.IsNullOrWhiteSpace(responseModel.AccessToken))
        {
            throw new InvalidOperationException("YouTube no devolvió un token de acceso.");
        }

        return new YouTubeTokenInfo
        {
            AccessToken = responseModel.AccessToken,
            RefreshToken = responseModel.RefreshToken ?? "",
            ExpiresAt = _timeProvider.GetUtcNow().AddSeconds(Math.Max(responseModel.ExpiresIn, 60)),
            Scopes = (responseModel.Scope ?? "")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        };
    }

    private static string CreateCodeVerifier() => CreateUrlSafeRandom(64);

    private static void AddClientSecret(IDictionary<string, string> fields, string clientSecret)
    {
        if (!string.IsNullOrWhiteSpace(clientSecret))
        {
            fields["client_secret"] = clientSecret.Trim();
        }
    }

    private static bool IsLoopbackHost(string host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || (IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address));

    private static string CreateUrlSafeRandom(int bytes) => Base64UrlEncode(RandomNumberGenerator.GetBytes(bytes));

    private static string Base64UrlEncode(byte[] value) => Convert.ToBase64String(value)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');

    private static string DescribeOAuthError(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var error = document.RootElement.TryGetProperty("error", out var errorValue)
                ? errorValue.GetString()
                : null;
            var description = document.RootElement.TryGetProperty("error_description", out var descriptionValue)
                ? descriptionValue.GetString()
                : null;
            var safeError = new string((error ?? "desconocido")
                .Where(character => char.IsLetterOrDigit(character) || character is '_' or '-')
                .Take(64)
                .ToArray());
            var safeDescription = new string((description ?? "")
                .Where(character => char.IsLetterOrDigit(character)
                    || character is ' ' or '-' or '_' or '.' or ':' or '/' or '=')
                .Take(180)
                .ToArray())
                .Trim();
            if (string.IsNullOrWhiteSpace(safeError))
            {
                return "error desconocido";
            }

            return string.IsNullOrWhiteSpace(safeDescription)
                ? $"error {safeError}"
                : $"error {safeError}: {safeDescription}";
        }
        catch
        {
            return "detalle remoto no disponible";
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0 && _ownsHttpClient)
        {
            _http.Dispose();
        }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = "";

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }
}

public sealed record YouTubeAuthorizationRequest(
    Uri AuthorizationUri,
    Uri RedirectUri,
    string State,
    string CodeVerifier);

public static class YouTubeTokenRefreshPolicy
{
    private static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(5);

    public static bool NeedsRefresh(YouTubeTokenInfo token, DateTimeOffset now) =>
        string.IsNullOrWhiteSpace(token.AccessToken) || token.ExpiresAt <= now.Add(RefreshWindow);
}
