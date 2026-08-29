using System.Net.WebSockets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoTwitch.Services.Obs;

namespace NeoTwitch.Tests;

[TestClass]
public sealed class ObsWebSocketFrameReaderTests
{
    [TestMethod]
    public async Task ValidFragmentedMessageUnderLimitSucceeds()
    {
        var receiver = new FakeFrameReceiver(
            Frame("{\"op\":"),
            FinalFrame("0}"));

        using var json = await ReadAsync(receiver, maximumBytes: 64);

        Assert.AreEqual(0, json.RootElement.GetProperty("op").GetInt32());
        Assert.IsFalse(receiver.Aborted);
    }

    [TestMethod]
    public async Task ExactLimitMessageSucceeds()
    {
        const int limit = 64;
        var bytes = ExactSizeJson(limit);
        var receiver = new FakeFrameReceiver(FinalFrame(bytes));

        using var json = await ReadAsync(receiver, limit);

        Assert.AreEqual(1, json.RootElement.GetProperty("ok").GetInt32());
        Assert.IsFalse(receiver.Aborted);
    }

    [TestMethod]
    public async Task OversizedSingleMessageFailsSafely()
    {
        const int limit = 16;
        var receiver = new FakeFrameReceiver(
            Frame(new byte[16]),
            FinalFrame(new byte[1]));

        await Assert.ThrowsExactlyAsync<ObsWebSocketMessageTooLargeException>(() => ReadAsync(receiver, limit));

        Assert.IsTrue(receiver.Aborted);
    }

    [TestMethod]
    public async Task FragmentedPayloadFailsBeforeFullAccumulation()
    {
        const int limit = 32;
        var receiver = new FakeFrameReceiver(
            Frame(new byte[16]),
            Frame(new byte[16]),
            Frame(new byte[16]),
            FinalFrame(new byte[16]));

        await Assert.ThrowsExactlyAsync<ObsWebSocketMessageTooLargeException>(() => ReadAsync(receiver, limit));

        Assert.AreEqual(3, receiver.ReceiveCount);
        Assert.AreEqual(1, receiver.RemainingFrames);
    }

    [TestMethod]
    public async Task OversizedFailureAbortsTransportExactlyOnce()
    {
        var receiver = new FakeFrameReceiver(Frame(new byte[8]), FinalFrame(new byte[1]));

        await Assert.ThrowsExactlyAsync<ObsWebSocketMessageTooLargeException>(() => ReadAsync(receiver, maximumBytes: 8));

        Assert.AreEqual(1, receiver.AbortCount);
        Assert.IsTrue(receiver.Aborted);
    }

    private static Task<System.Text.Json.JsonDocument> ReadAsync(FakeFrameReceiver receiver, int maximumBytes) =>
        ObsWebSocketFrameReader.ReceiveJsonAsync(
            receiver.ReceiveAsync,
            receiver.Abort,
            "closed",
            CancellationToken.None,
            maximumBytes);

    private static FakeFrame Frame(string value) => Frame(Encoding.UTF8.GetBytes(value));

    private static FakeFrame FinalFrame(string value) => FinalFrame(Encoding.UTF8.GetBytes(value));

    private static FakeFrame Frame(byte[] value) => new(value, EndOfMessage: false);

    private static FakeFrame FinalFrame(byte[] value) => new(value, EndOfMessage: true);

    private static byte[] ExactSizeJson(int size)
    {
        var json = "{\"ok\":1}";
        return Encoding.UTF8.GetBytes(json.PadRight(size));
    }

    private sealed class FakeFrameReceiver(params FakeFrame[] frames)
    {
        private readonly Queue<FakeFrame> _frames = new(frames);

        public int ReceiveCount { get; private set; }
        public int AbortCount { get; private set; }
        public bool Aborted => AbortCount > 0;
        public int RemainingFrames => _frames.Count;

        public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReceiveCount++;
            var frame = _frames.Dequeue();
            if (frame.Bytes.Length > buffer.Count)
            {
                throw new InvalidOperationException("Fake frame exceeds supplied receive buffer.");
            }

            frame.Bytes.CopyTo(buffer.Array!, buffer.Offset);
            return Task.FromResult(new WebSocketReceiveResult(
                frame.Bytes.Length,
                WebSocketMessageType.Text,
                frame.EndOfMessage));
        }

        public void Abort() => AbortCount++;
    }

    private sealed record FakeFrame(byte[] Bytes, bool EndOfMessage);
}
