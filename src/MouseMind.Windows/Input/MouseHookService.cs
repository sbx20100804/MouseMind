using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace MouseMind.Windows.Input;

public sealed class MouseHookService : IDisposable
{
    private const int WhMouseLl = 14;
    private const int WmXButtonDown = 0x020B;
    private const uint WmQuit = 0x0012;
    private readonly object _lifecycleGate = new();
    private readonly Channel<MouseInputEvent> _events = Channel.CreateBounded<MouseInputEvent>(
        new BoundedChannelOptions(128)
        {
            SingleWriter = true,
            SingleReader = true,
            FullMode = BoundedChannelFullMode.DropWrite
        });
    private Thread? _hookThread;
    private TaskCompletionSource<bool>? _started;
    private TaskCompletionSource<bool>? _stopped;
    private IntPtr _hook;
    private HookProc? _callback;
    private uint _hookThreadId;
    private bool _disposed;
    private long _droppedEvents;

    public event EventHandler<string>? Diagnostic;
    public bool IsRunning => _hook != IntPtr.Zero && _hookThread?.IsAlive == true;
    public long DroppedEvents => Interlocked.Read(ref _droppedEvents);

    public void Start()
    {
        lock (_lifecycleGate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (IsRunning) return;
            _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _hookThread = new Thread(HookThreadMain)
            {
                IsBackground = true,
                Name = "MouseMind.InputHook"
            };
            _hookThread.Start();
        }

        if (!(_started?.Task.Wait(TimeSpan.FromSeconds(3)) ?? false))
            throw new TimeoutException("鼠标钩子线程启动超时。");
        _started.Task.GetAwaiter().GetResult();
    }

    public bool Stop()
    {
        Task<bool>? stoppedTask;
        lock (_lifecycleGate)
        {
            if (_hookThread is null || !_hookThread.IsAlive) return true;
            if (_hookThreadId == 0 || !PostThreadMessage(_hookThreadId, WmQuit, UIntPtr.Zero, IntPtr.Zero))
            {
                Diagnostic?.Invoke(this, $"无法通知鼠标钩子线程退出，Win32={Marshal.GetLastWin32Error()}。");
                return false;
            }
            stoppedTask = _stopped?.Task;
        }

        if (stoppedTask is null || !stoppedTask.Wait(TimeSpan.FromSeconds(3)))
        {
            Diagnostic?.Invoke(this, "鼠标钩子线程停止超时。");
            return false;
        }
        return stoppedTask.GetAwaiter().GetResult();
    }

    public IAsyncEnumerable<MouseInputEvent> ReadEventsAsync(CancellationToken cancellationToken = default) =>
        _events.Reader.ReadAllAsync(cancellationToken);

    private void HookThreadMain()
    {
        var unhooked = true;
        try
        {
            _hookThreadId = GetCurrentThreadId();
            PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
            _callback = HookCallback;
            using var process = Process.GetCurrentProcess();
            using var module = process.MainModule!;
            _hook = SetWindowsHookEx(WhMouseLl, _callback, GetModuleHandle(module.ModuleName), 0);
            if (_hook == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                _started?.TrySetException(new InvalidOperationException($"全局鼠标监听器启动失败，Win32={error}。"));
                return;
            }

            _started?.TrySetResult(true);
            while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref message);
                DispatchMessage(ref message);
            }
        }
        catch (Exception ex)
        {
            _started?.TrySetException(ex);
            Diagnostic?.Invoke(this, $"鼠标钩子线程异常：{ex.Message}");
            unhooked = false;
        }
        finally
        {
            if (_hook != IntPtr.Zero)
            {
                unhooked = UnhookWindowsHookEx(_hook);
                if (!unhooked)
                    Diagnostic?.Invoke(this, $"鼠标钩子卸载失败，Win32={Marshal.GetLastWin32Error()}。");
            }
            _hook = IntPtr.Zero;
            _hookThreadId = 0;
            _callback = null;
            _stopped?.TrySetResult(unhooked);
        }
    }

    private IntPtr HookCallback(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0 && message.ToInt32() == WmXButtonDown)
        {
            try
            {
                var info = Marshal.PtrToStructure<MsllHookStruct>(data);
                var buttonData = (info.MouseData >> 16) & 0xffff;
                var trigger = buttonData switch { 1 => "侧键 1", 2 => "侧键 2", _ => null };
                if (trigger is not null && !_events.Writer.TryWrite(
                        new MouseInputEvent(trigger, DateTimeOffset.UtcNow, GetForegroundWindow())))
                {
                    var dropped = Interlocked.Increment(ref _droppedEvents);
                    if (dropped == 1 || dropped % 100 == 0)
                        Diagnostic?.Invoke(this, $"输入队列已满，累计丢弃 {dropped} 个事件。");
                }
            }
            catch
            {
                // Exceptions must never cross the unmanaged hook boundary.
            }
        }
        return CallNextHookEx(_hook, code, message, data);
    }

    public void Dispose()
    {
        lock (_lifecycleGate)
        {
            if (_disposed) return;
            _disposed = true;
        }
        Stop();
        _events.Writer.TryComplete();
    }

    private delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MsllHookStruct { public Point Point; public uint MouseData; public uint Flags; public uint Time; public UIntPtr ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public IntPtr Window; public uint Message; public UIntPtr WParam; public IntPtr LParam;
        public uint Time; public Point Point; public uint Private;
    }

    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, HookProc callback, IntPtr module, uint threadId);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll")] private static extern int GetMessage(out Msg message, IntPtr window, uint min, uint max);
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref Msg message);
    [DllImport("user32.dll")] private static extern IntPtr DispatchMessage(ref Msg message);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool PostThreadMessage(uint threadId, uint message, UIntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool PeekMessage(out Msg message, IntPtr window, uint min, uint max, uint remove);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)] private static extern IntPtr GetModuleHandle(string? moduleName);
}
