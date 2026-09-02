using System.Collections.Concurrent;
using MouseMind.Core.Models;

namespace MouseMind.Core.Actions;

public sealed class ActionExecutionService
{
    private const int PruneEvery = 64;
    private static readonly TimeSpan StateRetention = TimeSpan.FromMinutes(10);
    private readonly Dictionary<string, IActionExecutor> _executors;
    private readonly ConcurrentDictionary<string, ExecutionState> _states = new();
    private int _executionCount;

    public ActionExecutionService(IEnumerable<IActionExecutor> executors)
    {
        _executors = executors.ToDictionary(x => x.ActionType, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<ActionResult> ExecuteAsync(
        MouseMapping mapping, ActionContext context, CancellationToken cancellationToken = default)
    {
        if (!_executors.TryGetValue(mapping.ActionType, out var executor))
            return ActionResult.Skip($"动作“{mapping.Action}”目前处于预览模式。");

        var key = $"{context.ForegroundProcess}|{mapping.Trigger}|{mapping.ActionType}|{mapping.Payload}";
        var state = _states.GetOrAdd(key, _ => new ExecutionState());
        var now = DateTimeOffset.UtcNow;
        state.LastTouched = now;

        if (now - state.LastExecution < TimeSpan.FromMilliseconds(Math.Max(0, mapping.CooldownMs)))
            return ActionResult.Skip("动作处于冷却时间，已忽略重复触发。");

        try
        {
            if (!await state.Gate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
                return ActionResult.Skip("相同动作正在执行，已忽略重复触发。");
        }
        catch (OperationCanceledException)
        {
            return ActionResult.Cancel("动作已取消。");
        }

        state.LastExecution = now;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (mapping.TimeoutMs > 0) timeout.CancelAfter(mapping.TimeoutMs);

            try
            {
                return await executor.ExecuteAsync(mapping, context, timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return ActionResult.Timeout($"动作执行超过 {mapping.TimeoutMs}ms，已停止等待。");
            }
            catch (OperationCanceledException)
            {
                return ActionResult.Cancel("动作已取消。");
            }
            catch (Exception ex)
            {
                return ActionResult.Fail($"执行失败：{ex.Message}");
            }
        }
        finally
        {
            state.LastTouched = DateTimeOffset.UtcNow;
            state.Gate.Release();
            if (Interlocked.Increment(ref _executionCount) % PruneEvery == 0)
                PruneState(state.LastTouched);
        }
    }

    private void PruneState(DateTimeOffset now)
    {
        foreach (var pair in _states)
        {
            if (pair.Value.Gate.CurrentCount == 1 && now - pair.Value.LastTouched > StateRetention)
                _states.TryRemove(pair.Key, out _);
        }
    }

    private sealed class ExecutionState
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public DateTimeOffset LastExecution { get; set; } = DateTimeOffset.MinValue;
        public DateTimeOffset LastTouched { get; set; } = DateTimeOffset.UtcNow;
    }
}
