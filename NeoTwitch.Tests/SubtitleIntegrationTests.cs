using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Integrations;

namespace NeoTwitch.Tests;

[TestClass]
public sealed class SubtitleIntegrationTests
{
    [TestMethod]
    public async Task IndependentServersKeepSeparateTextAndLifetimes()
    {
        var captions = new SubtitleServer(new());
        await using var chat = new SubtitleServer(new());
        captions.Start(); chat.Start();
        using var http = new HttpClient();
        try
        {
            captions.SetText("Voz", "Voice"); chat.SetText("Chat", "Message");
            using var a = JsonDocument.Parse(await http.GetStringAsync(captions.BaseUrl + "state"));
            using var b = JsonDocument.Parse(await http.GetStringAsync(chat.BaseUrl + "state"));
            Assert.AreEqual("Voz", a.RootElement.GetProperty("original").GetString());
            Assert.AreEqual("Chat", b.RootElement.GetProperty("original").GetString());
        }
        finally { await captions.DisposeAsync(); }
        Assert.IsTrue((await http.GetStringAsync(chat.BaseUrl + "state")).Contains("Message"));
    }

    [TestMethod]
    public async Task ForeignOriginAndWrongSessionCannotReadSubtitles()
    {
        await using var server = new SubtitleServer(new()); server.Start();
        using var http = new HttpClient();
        using var originRequest = new HttpRequestMessage(HttpMethod.Get, server.BaseUrl + "state");
        originRequest.Headers.Add("Origin", "https://untrusted.example");
        using var originResult = await http.SendAsync(originRequest);
        Assert.AreEqual(HttpStatusCode.Forbidden, originResult.StatusCode);
        using var sessionResult = await http.GetAsync(new Uri(server.BaseUrl).GetLeftPart(UriPartial.Authority) + "/other/state");
        Assert.AreEqual(HttpStatusCode.Forbidden, sessionResult.StatusCode);
    }

    [TestMethod]
    public async Task CaptionPushPreservesUnicodeAndChatEndpointRejectsPush()
    {
        await using var captions = new SubtitleServer(new(), "unused-project"); captions.Start();
        await using var chat = new SubtitleServer(new()); chat.Start();
        using var http = new HttpClient();
        using var payload = new StringContent("{\"kind\":\"text\",\"original\":\"¿Cómo estás? 日本語\",\"translation\":\"Hello\",\"status\":\"OK\",\"listening\":true}", Encoding.UTF8);
        payload.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var result = await http.PostAsync(captions.BaseUrl + "push", payload);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        using var state = JsonDocument.Parse(await http.GetStringAsync(captions.BaseUrl + "state"));
        Assert.AreEqual("¿Cómo estás? 日本語", state.RootElement.GetProperty("original").GetString());
        Assert.IsTrue(captions.HasContact);
        using var denied = await http.PostAsync(chat.BaseUrl + "push", new StringContent("{}"));
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, denied.StatusCode);
    }

    [TestMethod]
    public void ChatSubscriptionExistsOnlyWhileChatIntegrationIsActive()
    {
        var config = new AppConfig();
        config.LiveCaptions.Running = true;
        Assert.AreEqual(0, TwitchEventSubSubscriptionPlanner.BuildDefinitions(config).Count);
        config.ChatTranslator.Running = true;
        Assert.AreEqual("channel.chat.message", TwitchEventSubSubscriptionPlanner.BuildDefinitions(config).Single().Type);
        config.ChatTranslator.Running = false;
        Assert.AreEqual(0, TwitchEventSubSubscriptionPlanner.BuildDefinitions(config).Count);
    }

    [TestMethod]
    public void SavedSettingsNeverReactivateEitherIntegration()
    {
        var config = new AppConfig();
        config.LiveCaptions.Running = config.ChatTranslator.Running = true;
        config.ChatTranslator.TargetLanguage = "fr";
        var restored = JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(config))!;
        Assert.IsFalse(restored.LiveCaptions.Running); Assert.IsFalse(restored.ChatTranslator.Running);
        Assert.AreEqual("fr", restored.ChatTranslator.TargetLanguage);
        Assert.AreEqual("en", restored.LiveCaptions.TargetLanguage);
    }

    [TestMethod]
    public void CaptionLeaseDoesNotBlockChatUninstall()
    {
        var root = Path.Combine(Path.GetTempPath(), "NeoTwitch-Subtitles-" + Guid.NewGuid().ToString("N"));
        try
        {
            var captions = new SayonariInstallation(true, Path.Combine(root, "captions"));
            var chat = new SayonariInstallation(false, Path.Combine(root, "chat"));
            using var lease = captions.AcquireLease();
            Directory.CreateDirectory(chat.Current);
            chat.Uninstall();
            Assert.IsFalse(Directory.Exists(chat.Current));
            Assert.ThrowsExactly<IOException>(() => captions.Uninstall());
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
