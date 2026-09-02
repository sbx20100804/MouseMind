using System.ComponentModel;
using System.Runtime.InteropServices;
using MouseMind.Core.Actions;
using MouseMind.Core.Models;

namespace MouseMind.Windows.Actions;

public interface IKeyboardInputSender
{
    uint Send(IReadOnlyList<ushort> keys);
    void ReleaseAll(IReadOnlyList<ushort> keys);
    int LastError { get; }
}

public sealed class KeyboardShortcutExecutor : IActionExecutor
{
    private readonly IKeyboardInputSender _sender;
    public KeyboardShortcutExecutor(IKeyboardInputSender? sender = null) => _sender = sender ?? new Win32KeyboardInputSender();
    public string ActionType => "KeyboardShortcut";

    public Task<ActionResult> ExecuteAsync(
        MouseMapping mapping, ActionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ShortcutParser.TryParse(mapping.Payload, out var keys, out var error))
            return Task.FromResult(ActionResult.Fail(error));

        var expected = (uint)(keys.Count * 2);
        var sent = _sender.Send(keys);
        if (sent != expected)
        {
            _sender.ReleaseAll(keys);
            var detail = _sender.LastError == 0 ? "未知系统错误" : new Win32Exception(_sender.LastError).Message;
            return Task.FromResult(ActionResult.Fail($"系统仅发送了 {sent}/{expected} 个键盘事件；已执行按键释放兜底。{detail}"));
        }

        return Task.FromResult(ActionResult.Ok($"已执行：{mapping.Action}（{mapping.Payload}）"));
    }
}

public sealed class Win32KeyboardInputSender : IKeyboardInputSender
{
    public int LastError { get; private set; }

    public uint Send(IReadOnlyList<ushort> keys)
    {
        var inputs = new List<Input>(keys.Count * 2);
        foreach (var key in keys) inputs.Add(CreateKeyInput(key, false));
        for (var i = keys.Count - 1; i >= 0; i--) inputs.Add(CreateKeyInput(keys[i], true));
        var sent = SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<Input>());
        LastError = sent == inputs.Count ? 0 : Marshal.GetLastWin32Error();
        return sent;
    }

    public void ReleaseAll(IReadOnlyList<ushort> keys)
    {
        var releases = new List<Input>(keys.Count);
        for (var i = keys.Count - 1; i >= 0; i--) releases.Add(CreateKeyInput(keys[i], true));
        if (releases.Count > 0) SendInput((uint)releases.Count, releases.ToArray(), Marshal.SizeOf<Input>());
    }

    private static Input CreateKeyInput(ushort key, bool keyUp) => new()
    {
        Type = 1,
        Union = new InputUnion
        {
            Keyboard = new KeyboardInput { VirtualKey = key, Flags = keyUp ? 0x0002u : 0u }
        }
    };

    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint inputCount, Input[] inputs, int size);
    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public InputUnion Union; }
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MouseInput Mouse;
        [FieldOffset(0)] public KeyboardInput Keyboard;
        [FieldOffset(0)] public HardwareInput Hardware;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput { public int X; public int Y; public uint MouseData; public uint Flags; public uint Time; public UIntPtr ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput { public ushort VirtualKey; public ushort ScanCode; public uint Flags; public uint Time; public UIntPtr ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareInput { public uint Message; public ushort ParameterLow; public ushort ParameterHigh; }
}
