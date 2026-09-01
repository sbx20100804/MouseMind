# MouseMind

[简体中文](README.zh-CN.md) · [Home](README.md)

MouseMind is a context-aware mouse control center for Windows. It automatically switches profiles based on the foreground application and turns side buttons, wheel input and gestures into shortcuts, automations or AI workflows.

## Why MouseMind

Traditional mouse software permanently binds one button to one function. MouseMind uses an application-context, trigger and action model, allowing the same button to behave differently in every app.

```text
Foreground app → Mouse trigger → Action or workflow → Feedback
```

Examples:

| Context | Side-button action |
|---|---|
| Browser | Translate or summarize selected text |
| Code editor | Undo, explain code or suggest a fix |
| Video editor | Split a clip or move frame-by-frame |
| Document editor | Rewrite text or extract tasks |

## Implemented in Alpha 0.2

- Dark Windows desktop control center
- Global XButton1/XButton2 listener
- Foreground process detection and profile matching
- Real keyboard shortcut execution through Windows `SendInput`
- Ctrl, Shift, Alt, Win, letters, digits and function-key combinations
- Per-action cooldown protection
- Application profiles and action mappings
- Local JSON persistence
- Live mouse and execution log
- Automated GitHub Actions builds

## Run locally

```powershell
dotnet build .\MouseMind.slnx
dotnet run --project .\src\MouseMind.App\MouseMind.App.csproj
```

Requirements: Windows and the .NET 10 SDK.

## Configuration

Profiles are stored at:

```text
%LocalAppData%\MouseMind\profiles.json
```

The built-in Code Workspace profile maps side button 2 to `Ctrl+Z`. Alpha 0.2 keeps the mouse button's original behavior for compatibility testing.

## Roadmap

- Alpha 0.3: full profile editor, system tray and action toast
- Alpha 0.4: selected-text capture, translation, summarization and AI providers
- Alpha 0.5: mouse gestures, radial menu and action chains
- Beta: plugin SDK, profile import/export, installer and auto-update

## Privacy

Basic input mapping and profile matching run locally. Future AI features will clearly disclose data flow and support local providers.

## License

[MIT License](LICENSE)

