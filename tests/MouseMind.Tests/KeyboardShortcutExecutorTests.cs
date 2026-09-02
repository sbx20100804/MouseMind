using MouseMind.Core.Actions;
using MouseMind.Core.Models;
using MouseMind.Windows.Actions;

namespace MouseMind.Tests;

public sealed class KeyboardShortcutExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsSuccessWhenAllEventsAreSent()
    {
        var sender = new FakeSender { Sent = 4 };
        var executor = new KeyboardShortcutExecutor(sender);
        var result = await executor.ExecuteAsync(Mapping("Ctrl+Z"), Context(), CancellationToken.None);
        Assert.True(result.Success);
        Assert.Equal(0, sender.ReleaseCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ReleasesKeysAfterPartialSend()
    {
        var sender = new FakeSender { Sent = 1, LastError = 5 };
        var executor = new KeyboardShortcutExecutor(sender);
        var result = await executor.ExecuteAsync(Mapping("Ctrl+Z"), Context(), CancellationToken.None);
        Assert.Equal(ActionStatus.Failed, result.Status);
        Assert.Equal(1, sender.ReleaseCalls);
    }

    private static MouseMapping Mapping(string payload) => new()
    {
        Action = "Shortcut", ActionType = "KeyboardShortcut", Payload = payload
    };
    private static ActionContext Context() => new("Code", DateTimeOffset.UtcNow);

    private sealed class FakeSender : IKeyboardInputSender
    {
        public uint Sent { get; init; }
        public int LastError { get; init; }
        public int ReleaseCalls { get; private set; }
        public uint Send(IReadOnlyList<ushort> keys) => Sent;
        public void ReleaseAll(IReadOnlyList<ushort> keys) => ReleaseCalls++;
    }
}

