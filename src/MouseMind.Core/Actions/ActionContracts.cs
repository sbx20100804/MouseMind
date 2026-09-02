using MouseMind.Core.Models;

namespace MouseMind.Core.Actions;

public sealed record ActionContext(string ForegroundProcess, DateTimeOffset TriggeredAt);

public enum ActionStatus
{
    Success,
    Failed,
    Skipped,
    Cancelled,
    TimedOut
}

public sealed record ActionResult(ActionStatus Status, string Message)
{
    public bool Success => Status == ActionStatus.Success;
    public static ActionResult Ok(string message) => new(ActionStatus.Success, message);
    public static ActionResult Fail(string message) => new(ActionStatus.Failed, message);
    public static ActionResult Skip(string message) => new(ActionStatus.Skipped, message);
    public static ActionResult Cancel(string message) => new(ActionStatus.Cancelled, message);
    public static ActionResult Timeout(string message) => new(ActionStatus.TimedOut, message);
}

public interface IActionExecutor
{
    string ActionType { get; }
    Task<ActionResult> ExecuteAsync(MouseMapping mapping, ActionContext context, CancellationToken cancellationToken);
}

