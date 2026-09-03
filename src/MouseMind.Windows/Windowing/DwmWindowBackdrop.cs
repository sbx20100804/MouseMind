using System.Runtime.InteropServices;

namespace MouseMind.Windows.Windowing;

public enum SystemBackdropKind
{
    None = 1,
    Mica = 2,
    Acrylic = 3,
    MicaAlt = 4
}

public static class DwmWindowBackdrop
{
    private const int UseImmersiveDarkMode = 20;
    private const int WindowCornerPreference = 33;
    private const int BorderColor = 34;
    private const int SystemBackdropType = 38;
    private const int CornerRound = 2;
    private const int ColorNone = unchecked((int)0xFFFFFFFEu);

    public static bool IsSystemBackdropSupported =>
        OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621);

    public static void ConfigureFrame(nint window, bool dark)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
        Set(window, UseImmersiveDarkMode, dark ? 1 : 0);
        Set(window, WindowCornerPreference, CornerRound);
        Set(window, BorderColor, ColorNone);
    }

    public static bool TrySetBackdrop(nint window, SystemBackdropKind backdrop) =>
        IsSystemBackdropSupported && Set(window, SystemBackdropType, (int)backdrop);

    private static bool Set(nint window, int attribute, int value)
    {
        if (window == 0) return false;
        try { return DwmSetWindowAttribute(window, attribute, ref value, sizeof(int)) >= 0; }
        catch (DllNotFoundException) { return false; }
        catch (EntryPointNotFoundException) { return false; }
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int valueSize);
}
