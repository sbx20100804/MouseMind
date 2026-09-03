# MouseMind Liquid Glass

MouseMind uses a Windows-native interpretation of Apple’s material and motion principles. It does not copy macOS controls or proprietary assets; it applies the same ideas of translucency, hierarchy, restraint and purposeful feedback to a Windows desktop utility.

## Material hierarchy

```text
L0  Windows Desktop Acrylic backdrop
L1  translucent title bar and navigation pane
L2  stable glass content groups
L3  elevated toast and temporary surfaces
```

- The app applies one system backdrop to the window rather than stacking blur effects.
- On supported Windows 11 versions, Desktop Acrylic is preferred, then Mica Alt and Mica.
- Unsupported systems retain an opaque fallback background.
- WPF `AllowsTransparency` remains disabled so DWM composition, resizing and hardware acceleration keep working.
- Foreground text always sits on a sufficiently dark tint instead of directly on arbitrary wallpaper colors.

## Visual language

- System blue is the only brand/action accent.
- Green, yellow and red are reserved for status semantics.
- Navigation selection uses a quiet translucent capsule, not a neon rail.
- Technical badges, decorative all-caps labels and permanent glow were removed.
- `Segoe UI Variable` provides a platform-appropriate alternative to proprietary Apple fonts.
- Large groups use 18px corners, inset groups 13px and controls 10px.

## Motion

- No infinite decorative animation.
- Page content enters with a short fade and 6px translation.
- The context lens responds only to real input.
- Toasts enter with 8px translation and always dismiss, including when Windows animations are disabled.
- Windows reduced-motion preferences are respected.

## Accessibility

- Icon-only title-bar buttons have automation names.
- Switches have accessible labels.
- Custom focus states use a visible 2px border.
- Status is communicated with text and icons, not color alone.
- The minimum window size is reduced for smaller logical displays.

## References

- [Apple Human Interface Guidelines — Materials](https://developer.apple.com/design/human-interface-guidelines/materials)
- [Apple Human Interface Guidelines — Motion](https://developer.apple.com/design/human-interface-guidelines/motion)
- [Apple Human Interface Guidelines — Dark Mode](https://developer.apple.com/design/human-interface-guidelines/dark-mode)
- [Windows materials](https://learn.microsoft.com/windows/apps/develop/ui/materials)
- [DWM window attributes](https://learn.microsoft.com/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute)

