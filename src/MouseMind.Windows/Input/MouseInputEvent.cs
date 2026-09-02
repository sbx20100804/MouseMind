namespace MouseMind.Windows.Input;

public sealed record MouseInputEvent(string Trigger, DateTimeOffset Timestamp, nint ForegroundWindow);
