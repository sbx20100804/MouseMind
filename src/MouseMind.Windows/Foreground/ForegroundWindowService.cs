using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MouseMind.Windows.Foreground;

public sealed class ForegroundWindowService
{
    public nint CurrentWindow => GetForegroundWindow();

    public bool IsStillForeground(nint window) => window != 0 && GetForegroundWindow() == window;

    public string GetProcessName(nint window)
    {
        GetWindowThreadProcessId(window, out var processId);
        try { return Process.GetProcessById((int)processId).ProcessName; }
        catch { return "未知应用"; }
    }

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}
