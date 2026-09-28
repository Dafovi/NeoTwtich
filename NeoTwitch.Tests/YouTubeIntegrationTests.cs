using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using NeoTwitch.Models;
using NeoTwitch.Services.Ui;
using NeoTwitch.Services.Streaming;
using NeoTwitch.Services.YouTube;

static class YouTubeIntegrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    public static void BuildsPkceAuthorizationRequest()
    {
        using var service = new YouTubeAuthService(new NullExternalLauncher(), new FixedTimeProvider(Now));
        var request = service.BeginAuthorization(
            "desktop-client.apps.googleusercontent.com",
            new Uri("http://127.0.0.1:54321/"));

        TestAssert.True(request.AuthorizationUri.AbsoluteUri.StartsWith("https://accounts.google.com/", StringComparison.Ordinal));
        TestAssert.True(request.CodeVerifier.Length >= 43);
        TestAssert.Equal(YouTubeAuthService.CreateCodeChallenge(request.CodeVerifier),
            ParseQuery(request.AuthorizationUri)["code_challenge"]);
        TestAssert.Equal(request.State, ParseQuery(request.AuthorizationUri)["state"]);
        TestAssert.Equal("S256", ParseQuery(request.AuthorizationUri)["code_challenge_method"]);
    }

    public static void ExchangesAndRefreshesTokens()
    {
        var handler = new TokenHandler();
        using var http = new HttpClient(handler);
        using var service = new YouTubeAuthService(new NullExternalLauncher(), new FixedTimeProvider(Now), http);
        var request = service.BeginAuthorization("client-id", new Uri("http://127.0.0.1:54321/"));
        var token = service.ExchangeAuthorizationCodeAsync("client-id", "desktop-client-secret", request, "callback-code", CancellationToken.None)
            .GetAwaiter().GetResult();

        TestAssert.Contains("code_verifier=", handler.RequestBodies[0]);
        TestAssert.Contains("code=callback-code", handler.RequestBodies[0]);
        TestAssert.Contains("client_secret=desktop-client-secret", handler.RequestBodies[0]);
        TestAssert.Equal("new-access", token.AccessToken);
        TestAssert.Equal("new-refresh", token.RefreshToken);
        TestAssert.Equal(Now.AddSeconds(3600), token.ExpiresAt);

        var config = TestConfig.CreateDefault();
        config.YouTubeClientId = "client-id";
        config.YouTubeClientSecret = "desktop-client-secret";
        config.YouTubeToken = new YouTubeTokenInfo
        {
            AccessToken = "expired",
            RefreshToken = "keep-this-refresh-token",
            ExpiresAt = Now.AddMinutes(1)
        };
        service.EnsureValidTokenAsync(config, CancellationToken.None).GetAwaiter().GetResult();

        TestAssert.Contains("grant_type=refresh_token", handler.RequestBodies[1]);
        TestAssert.Contains("client_secret=desktop-client-secret", handler.RequestBodies[1]);
        TestAssert.Equal("refreshed-access", config.YouTubeToken.AccessToken);
        TestAssert.Equal("keep-this-refresh-token", config.YouTubeToken.RefreshToken);
    }

    public static void ResolvesActiveBroadcast()
    {
        const string response = """
            {"items":[
                {"id":"finished-456","status":{"lifeCycleStatus":"complete"},"snippet":{"title":"Directo anterior"}},
                {"id":"broadcast-123","status":{"lifeCycleStatus":"live"},"snippet":{"title":"Mi directo","liveChatId":"chat-789"}}
            ]}
            """;
        using var http = new HttpClient(new JsonHandler(response));
        using var service = new YouTubeLiveService(http);

        var status = service.GetActiveBroadcastAsync(new YouTubeTokenInfo { AccessToken = "access" }, CancellationToken.None)
            .GetAwaiter().GetResult();

        TestAssert.True(status.IsLive);
        TestAssert.Equal("broadcast-123", status.BroadcastId);
        TestAssert.Equal("Mi directo", status.Title);
        TestAssert.Equal("chat-789", status.LiveChatId);
    }

    public static void ResolvesCurrentChannel()
    {
        const string response = """
            {"items":[{"id":"channel-123","snippet":{"title":"Neo Streamer","thumbnails":{"medium":{"url":"https://example.test/avatar.png"}}}}]}
            """;
        using var http = new HttpClient(new JsonHandler(response));
        using var service = new YouTubeChannelService(http);

        var channel = service.GetCurrentChannelAsync(new YouTubeTokenInfo { AccessToken = "access" }, CancellationToken.None)
            .GetAwaiter().GetResult();

        TestAssert.Equal("channel-123", channel.ChannelId);
        TestAssert.Equal("Neo Streamer", channel.DisplayName);
        TestAssert.Equal("https://example.test/avatar.png", channel.ThumbnailUrl);
    }

    public static void ReadsAndMapsLiveChatEvents()
    {
        const string response = """
            {"nextPageToken":"next-page","pollingIntervalMillis":2500,"items":[
              {"id":"message-1","authorDetails":{"displayName":"Ana"},"snippet":{"type":"textMessageEvent","publishedAt":"2026-09-26T12:00:00Z","textMessageDetails":{"messageText":"!hola"}}},
              {"id":"message-2","authorDetails":{"displayName":"Beto"},"snippet":{"type":"superChatEvent","publishedAt":"2026-09-26T12:00:01Z","superChatDetails":{"tier":3,"amountDisplayString":"$5.00","currency":"USD","amountMicros":"5000000"}}},
              {"id":"message-3","authorDetails":{"displayName":"Cami"},"snippet":{"type":"newSponsorEvent","publishedAt":"2026-09-26T12:00:02Z","newSponsorDetails":{"memberLevelName":"Nivel oro"}}}
            ]}
            """;
        using var http = new HttpClient(new JsonHandler(response));
        using var service = new YouTubeLiveChatService(http);

        var page = service.GetMessagesAsync(new YouTubeTokenInfo { AccessToken = "access" }, "chat-123", "previous-page", CancellationToken.None)
            .GetAwaiter().GetResult();

        TestAssert.Equal("next-page", page.NextPageToken);
        TestAssert.Equal(TimeSpan.FromMilliseconds(2500), page.PollingInterval);
        TestAssert.Equal(3, page.Messages.Count);

        var chat = YouTubeStreamEventAdapter.FromLiveChatMessage(page.Messages[0]);
        var superChat = YouTubeStreamEventAdapter.FromLiveChatMessage(page.Messages[1]);
        var membership = YouTubeStreamEventAdapter.FromLiveChatMessage(page.Messages[2]);
        TestAssert.Equal(StreamEventKind.ChatCommand, chat!.Kind);
        TestAssert.Equal("!hola", chat.Message!);
        TestAssert.Equal(StreamEventKind.PlatformCurrency, superChat!.Kind);
        TestAssert.Equal(3, superChat.ContributionUnits!.Value);
        TestAssert.Equal(StreamEventKind.Subscription, membership!.Kind);
        TestAssert.Equal("Nivel oro", membership.RewardTitle!);
    }

    public static void SendsMessagesToTheActiveLiveChat()
    {
        var handler = new CapturingJsonHandler();
        using var http = new HttpClient(handler);
        using var service = new YouTubeLiveChatService(http);

        service.SendMessageAsync(
                new YouTubeTokenInfo { AccessToken = "access" },
                "chat-123",
                "  Gracias por apoyar  ",
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        TestAssert.Equal(HttpMethod.Post, handler.Method);
        TestAssert.Equal("Bearer", handler.AuthorizationScheme);
        TestAssert.Equal("access", handler.AuthorizationParameter);
        TestAssert.Contains("\"liveChatId\":\"chat-123\"", handler.RequestBody);
        TestAssert.Contains("\"messageText\":\"Gracias por apoyar\"", handler.RequestBody);
    }

    public static void ReceivesLoopbackCallback()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listener = YouTubeLoopbackCallbackListener.Start();
        try
        {
            var callback = listener.WaitForAuthorizationCodeAsync("state-value", cancellation.Token);
            using var client = new TcpClient();
            client.Connect("127.0.0.1", listener.RedirectUri.Port);
            using var stream = client.GetStream();
            var request = Encoding.ASCII.GetBytes("GET /?code=callback-code&state=state-value HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n");
            stream.Write(request, 0, request.Length);

            TestAssert.Equal("callback-code", callback.GetAwaiter().GetResult());
        }
        finally
        {
            listener.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    private static Dictionary<string, string> ParseQuery(Uri uri) => uri.Query.TrimStart('?')
        .Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(pair => pair.Split('=', 2))
        .ToDictionary(pair => Uri.UnescapeDataString(pair[0]), pair => Uri.UnescapeDataString(pair[1]));

    private sealed class TokenHandler : HttpMessageHandler
    {
        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            RequestBodies.Add(body);
            var content = body.Contains("grant_type=refresh_token", StringComparison.Ordinal)
                ? """{"access_token":"refreshed-access","expires_in":1800,"scope":"https://www.googleapis.com/auth/youtube.readonly"}"""
                : """{"access_token":"new-access","refresh_token":"new-refresh","expires_in":3600,"scope":"https://www.googleapis.com/auth/youtube.readonly"}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) };
        }
    }

    private sealed class JsonHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
    }

    private sealed class CapturingJsonHandler : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public string AuthorizationScheme { get; private set; } = "";
        public string AuthorizationParameter { get; private set; } = "";
        public string RequestBody { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            AuthorizationScheme = request.Headers.Authorization?.Scheme ?? "";
            AuthorizationParameter = request.Headers.Authorization?.Parameter ?? "";
            RequestBody = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        }
    }

    private sealed class NullExternalLauncher : IExternalLauncherService
    {
        public void Open(string target) { }

        public void Launch(string fileName, string arguments = "", string? workingDirectory = null) { }
    }
}
