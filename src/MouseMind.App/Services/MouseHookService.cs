using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace MouseMind.App.Services;

public sealed class MouseHookService : IDisposable
{
    private const int WhMouseLl = 14;
    private const int WmXButtonDown = 0x020B;
    private IntPtr _hook;
    private HookProc? _callback;

    public event EventHandler<MouseSideButtonEventArgs>? SideButtonPressed;
    public bool IsRunning => _hook != IntPtr.Zero;

    public void Start()
    {
        if (IsRunning) return;
        _callback = HookCallback;
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        _hook = SetWindowsHookEx(WhMouseLl, _callback, GetModuleHandle(module.ModuleName), 0);
        if (_hook == IntPtr.Zero) throw new InvalidOperationException("全局鼠标监听器启动失败。");
    }

    public void Stop()
    {
        if (!IsRunning) return;
        UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
        _callback = null;
    }

    private IntPtr HookCallback(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0 && message.ToInt32() == WmXButtonDown)
        {
            var info = Marshal.PtrToStructure<MsllHookStruct>(data);
            var button = ((info.MouseData >> 16) & 0xffff) == 1 ? "侧键 1" : "侧键 2";
            var processName = GetForegroundProcessName();
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
                SideButtonPressed?.Invoke(this, new MouseSideButtonEventArgs(button, processName)));
        }
        return CallNextHookEx(_hook, code, message, data);
    }

    private static string GetForegroundProcessName()
    {
        GetWindowThreadProcessId(GetForegroundWindow(), out var processId);
        try { return Process.GetProcessById((int)processId).ProcessName; }
        catch { return "未知应用"; }
    }

    public void Dispose() => Stop();

    private delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MsllHookStruct
    {
        public Point Point; public uint MouseData; public uint Flags; public uint Time; public UIntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc callback, IntPtr module, uint threadId);
    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string? moduleName);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}

public sealed record MouseSideButtonEventArgs(string Button, string ProcessName);

