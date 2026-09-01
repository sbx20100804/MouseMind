using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using MouseMind.App.Models;

namespace MouseMind.App.Services;

public sealed record ActionContext(string ForegroundProcess, DateTimeOffset TriggeredAt);
public sealed record ActionResult(bool Success, string Message)
{
    public static ActionResult Ok(string message) => new(true, message);
    public static ActionResult Fail(string message) => new(false, message);
}

public interface IActionExecutor
{
    string ActionType { get; }
    Task<ActionResult> ExecuteAsync(MouseMapping mapping, ActionContext context, CancellationToken cancellationToken);
}

public sealed class ActionExecutionService
{
    private readonly Dictionary<string, IActionExecutor> _executors;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastExecution = new();

    public ActionExecutionService(IEnumerable<IActionExecutor> executors)
    {
        _executors = executors.ToDictionary(x => x.ActionType, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<ActionResult> ExecuteAsync(
        MouseMapping mapping, ActionContext context, CancellationToken cancellationToken = default)
    {
        var key = $"{context.ForegroundProcess}|{mapping.Trigger}|{mapping.ActionType}|{mapping.Payload}";
        var now = DateTimeOffset.UtcNow;
        if (_lastExecution.TryGetValue(key, out var last) &&
            now - last < TimeSpan.FromMilliseconds(Math.Max(0, mapping.CooldownMs)))
            return ActionResult.Fail("动作处于冷却时间，已忽略重复触发。");

        if (!_executors.TryGetValue(mapping.ActionType, out var executor))
            return ActionResult.Fail($"动作“{mapping.Action}”目前处于预览模式。");

        _lastExecution[key] = now;
        try { return await executor.ExecuteAsync(mapping, context, cancellationToken); }
        catch (Exception ex) { return ActionResult.Fail($"执行失败：{ex.Message}"); }
    }
}

public sealed class KeyboardShortcutExecutor : IActionExecutor
{
    public string ActionType => "KeyboardShortcut";

    public Task<ActionResult> ExecuteAsync(
        MouseMapping mapping, ActionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ShortcutParser.TryParse(mapping.Payload, out var keys, out var error))
            return Task.FromResult(ActionResult.Fail(error));

        var inputs = new List<Input>();
        foreach (var key in keys) inputs.Add(CreateKeyInput(key, false));
        for (var i = keys.Count - 1; i >= 0; i--) inputs.Add(CreateKeyInput(keys[i], true));

        var sent = SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<Input>());
        if (sent != inputs.Count)
            return Task.FromResult(ActionResult.Fail($"系统仅发送了 {sent}/{inputs.Count} 个键盘事件。"));

        return Task.FromResult(ActionResult.Ok($"已执行：{mapping.Action}（{mapping.Payload}）"));
    }

    private static Input CreateKeyInput(ushort key, bool keyUp) => new()
    {
        Type = 1,
        Union = new InputUnion
        {
            Keyboard = new KeyboardInput { VirtualKey = key, Flags = keyUp ? 0x0002u : 0u }
        }
    };

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int size);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input { public uint Type; public InputUnion Union; }
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MouseInput Mouse;
        [FieldOffset(0)] public KeyboardInput Keyboard;
        [FieldOffset(0)] public HardwareInput Hardware;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int X; public int Y; public uint MouseData; public uint Flags;
        public uint Time; public UIntPtr ExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey; public ushort ScanCode; public uint Flags;
        public uint Time; public UIntPtr ExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareInput { public uint Message; public ushort ParameterLow; public ushort ParameterHigh; }
}

internal static class ShortcutParser
{
    private static readonly Dictionary<string, ushort> NamedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CTRL"] = 0x11, ["CONTROL"] = 0x11, ["SHIFT"] = 0x10,
        ["ALT"] = 0x12, ["WIN"] = 0x5B, ["WINDOWS"] = 0x5B,
        ["ENTER"] = 0x0D, ["RETURN"] = 0x0D, ["ESC"] = 0x1B,
        ["ESCAPE"] = 0x1B, ["TAB"] = 0x09, ["SPACE"] = 0x20,
        ["BACKSPACE"] = 0x08, ["DELETE"] = 0x2E, ["HOME"] = 0x24,
        ["END"] = 0x23, ["LEFT"] = 0x25, ["UP"] = 0x26,
        ["RIGHT"] = 0x27, ["DOWN"] = 0x28
    };

    public static bool TryParse(string shortcut, out List<ushort> keys, out string error)
    {
        keys = [];
        error = "";
        var tokens = shortcut.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) { error = "快捷键内容为空。"; return false; }

        foreach (var token in tokens)
        {
            if (NamedKeys.TryGetValue(token, out var named)) { keys.Add(named); continue; }
            if (token.Length == 1 && char.IsLetterOrDigit(token[0]))
            {
                keys.Add(char.ToUpperInvariant(token[0]));
                continue;
            }
            if (token.Length is 2 or 3 && token.StartsWith("F", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(token[1..], out var functionKey) && functionKey is >= 1 and <= 24)
            {
                keys.Add((ushort)(0x70 + functionKey - 1));
                continue;
            }
            error = $"不支持的快捷键：{token}";
            keys.Clear();
            return false;
        }
        return true;
    }
}
