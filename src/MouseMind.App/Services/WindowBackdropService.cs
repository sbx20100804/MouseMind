using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using MouseMind.Windows.Windowing;

namespace MouseMind.App.Services;

public enum WindowBackdropKind
{
    Solid,
    Mica,
    Acrylic
}

public static class WindowBackdropService
{
    public static WindowBackdropKind Apply(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var source = HwndSource.FromHwnd(handle);
        var chrome = WindowChrome.GetWindowChrome(window);
        if (handle == 0 || source?.CompositionTarget is null || chrome is null)
            return WindowBackdropKind.Solid;

        if (chrome.IsFrozen)
        {
            chrome = (WindowChrome)chrome.Clone();
            WindowChrome.SetWindowChrome(window, chrome);
        }

        var oldThickness = chrome.GlassFrameThickness;
        var oldBackground = window.Background;
        var oldCompositionColor = source.CompositionTarget.BackgroundColor;
        chrome.GlassFrameThickness = WindowChrome.GlassFrameCompleteThickness;
        DwmWindowBackdrop.ConfigureFrame(handle, dark: true);

        var kind = DwmWindowBackdrop.TrySetBackdrop(handle, SystemBackdropKind.Acrylic)
            ? WindowBackdropKind.Acrylic
            : DwmWindowBackdrop.TrySetBackdrop(handle, SystemBackdropKind.MicaAlt) ||
              DwmWindowBackdrop.TrySetBackdrop(handle, SystemBackdropKind.Mica)
                ? WindowBackdropKind.Mica
                : WindowBackdropKind.Solid;

        if (kind == WindowBackdropKind.Solid)
        {
            chrome.GlassFrameThickness = oldThickness;
            window.Background = oldBackground;
            source.CompositionTarget.BackgroundColor = oldCompositionColor;
            return kind;
        }

        source.CompositionTarget.BackgroundColor = Colors.Transparent;
        window.Background = Brushes.Transparent;
        return kind;
    }
}
