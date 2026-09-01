# MouseMind · 灵触

> Context-aware mouse automation and AI workflow control center for Windows.<br>
> 面向 Windows 的上下文感知鼠标自动化与 AI 工作流控制中心。

[简体中文](README.zh-CN.md) · [English](README.en.md)

MouseMind turns standard mouse side buttons into application-aware actions. The same button can undo in a code editor, control a timeline in an editing app, or trigger an AI workflow in a browser.

MouseMind 将标准鼠标侧键升级为能够感知当前应用的动作入口。同一个按键可以在代码编辑器中撤销、在剪辑软件中控制时间线，或在浏览器中触发 AI 工作流。

## Alpha 0.3 · Prism (in development)

- Premium Obsidian Glass desktop shell
- Dashboard, profiles, activity and settings navigation
- Reusable WPF design tokens and accessible focus states
- Global XButton1/XButton2 listener
- Foreground application detection and profile matching
- Real keyboard shortcut execution through Windows `SendInput`
- Per-action cooldown protection
- Local JSON profile persistence
- Chinese and English documentation
- Automated Windows builds with GitHub Actions

## Quick start

```powershell
dotnet build .\MouseMind.slnx
dotnet run --project .\src\MouseMind.App\MouseMind.App.csproj
```

Requires Windows and .NET 10 SDK.

## Status

MouseMind is an early public alpha. Prism introduces the product-grade desktop foundation while keeping Alpha 0.2 shortcut execution. AI, OCR, gesture recognition and the full profile editor remain planned.

## License

[MIT](LICENSE)
