using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Text;

namespace NeoTwitch.Tests;

[TestClass]
public sealed class CriticalBoundaryTests
{
    [TestMethod]
    public void DefaultCompositionCreatesAndDisposesRequiredRuntimeServices()
    {
        var services = AppServices.CreateDefault();
        try
        {
            Assert.IsNotNull(services.SettingsStore);
            Assert.IsNotNull(services.AuthService);
            Assert.IsNotNull(services.ChatService);
            Assert.IsNotNull(services.ObsService);
            Assert.IsNotNull(services.LightController);
            Assert.IsNotNull(services.AlertQueue);
            Assert.IsNotNull(services.AlertExecutionTracker);
            Assert.AreSame(services.ActivityLog.DashboardEntries, services.ActivityViewModel.DashboardEntries);
        }
        finally
        {
            services.SettingsStore.Dispose();
            services.ChatService.Dispose();
            services.LightController.Dispose();
            services.ObsService.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    [TestMethod]
    public async Task AlertWaitHonorsHostCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() =>
            AlertEffectWaitService.WaitAsync(
                playback: null,
                command: null,
                obsMediaHides: [],
                cancellation.Token));
    }

    [TestMethod]
    public async Task ObsTransportOperationFailsClosedWhenDisconnected()
    {
        await using var service = new ObsWebSocketService(UiTextService.CreateDefault());

        var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.RefreshScenesAsync(CancellationToken.None));

        StringAssert.Contains(error.Message, "OBS");
        Assert.IsFalse(service.IsConnected);
    }

    [TestMethod]
    public void SafeModeSuppressesStartupSideEffects()
    {
        var options = AppStartupOptions.Parse(["--safe-mode"]);

        Assert.IsTrue(options.SafeMode);
        Assert.IsTrue(options.SuppressAutoConnect);
        Assert.IsTrue(options.SuppressStartHidden);
    }
}
