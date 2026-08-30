using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoTwitch.Services;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;

namespace NeoTwitch.Tests;

[TestClass]
public sealed class CrashLogRedactionBoundaryTests
{
    [TestMethod]
    public async Task TwitchAuthFailureOmitsRemoteResponseBody()
    {
        const string remoteSecret = "server-echoed-access-token-secret";
        using var http = new HttpClient(new FailureHandler(remoteSecret));
        using var service = new TwitchAuthService(
            UiTextService.CreateDefault(),
            new NullLauncher(),
            TimeProvider.System,
            http);

        var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.BeginDeviceFlowAsync("client-id", CancellationToken.None));

        Assert.IsFalse(error.Message.Contains(remoteSecret, StringComparison.Ordinal));
        StringAssert.Contains(error.Message, "HTTP 500");
    }

    private sealed class FailureHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(body)
            });
    }

    private sealed class NullLauncher : IExternalLauncherService
    {
        public void Open(string target) { }
        public void Launch(string fileName, string arguments = "", string? workingDirectory = null) { }
    }
}
