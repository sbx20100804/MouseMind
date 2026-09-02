using MouseMind.Core.Actions;
using MouseMind.Core.Models;

namespace MouseMind.Tests;

public sealed class ActionExecutionServiceTests
{
    [Fact]
    public async Task ExecuteAsync_EnforcesCooldown()
    {
        var calls = 0;
        var service = CreateService((_, _, _) =>
        {
            calls++;
            return Task.FromResult(ActionResult.Ok("ok"));
        });
        var mapping = Mapping(cooldown: 60_000);

        var first = await service.ExecuteAsync(mapping, Context());
        var second = await service.ExecuteAsync(mapping, Context());

        Assert.True(first.Success);
        Assert.Equal(ActionStatus.Skipped, second.Status);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task ExecuteAsync_PreventsReentrancy()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = CreateService(async (_, _, token) =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(token);
            return ActionResult.Ok("ok");
        });
        var mapping = Mapping(cooldown: 0);

        var first = service.ExecuteAsync(mapping, Context());
        await entered.Task;
        var second = await service.ExecuteAsync(mapping, Context());
        release.TrySetResult();

        Assert.Equal(ActionStatus.Skipped, second.Status);
        Assert.True((await first).Success);
    }

    [Fact]
    public async Task ExecuteAsync_DistinguishesTimeout()
    {
        var service = CreateService(async (_, _, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return ActionResult.Ok("never");
        });

        var result = await service.ExecuteAsync(Mapping(timeout: 30), Context());
        Assert.Equal(ActionStatus.TimedOut, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_DistinguishesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = CreateService((_, _, _) => Task.FromResult(ActionResult.Ok("never")));

        var result = await service.ExecuteAsync(Mapping(), Context(), cancellation.Token);
        Assert.Equal(ActionStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ConvertsExecutorExceptionToFailure()
    {
        var service = CreateService((_, _, _) => throw new InvalidOperationException("boom"));
        var result = await service.ExecuteAsync(Mapping(), Context());
        Assert.Equal(ActionStatus.Failed, result.Status);
        Assert.Contains("boom", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsUnknownActionType()
    {
        var service = new ActionExecutionService([]);
        var result = await service.ExecuteAsync(Mapping(), Context());
        Assert.Equal(ActionStatus.Skipped, result.Status);
    }

    private static ActionExecutionService CreateService(
        Func<MouseMapping, ActionContext, CancellationToken, Task<ActionResult>> callback) =>
        new([new DelegateExecutor(callback)]);

    private static MouseMapping Mapping(int cooldown = 0, int timeout = 5000) => new()
    {
        Trigger = "侧键 1", Action = "Test", ActionType = "Test", Payload = "payload",
        CooldownMs = cooldown, TimeoutMs = timeout
    };

    private static ActionContext Context() => new("Code", DateTimeOffset.UtcNow);

    private sealed class DelegateExecutor(
        Func<MouseMapping, ActionContext, CancellationToken, Task<ActionResult>> callback) : IActionExecutor
    {
        public string ActionType => "Test";
        public Task<ActionResult> ExecuteAsync(MouseMapping mapping, ActionContext context, CancellationToken cancellationToken) =>
            callback(mapping, context, cancellationToken);
    }
}

