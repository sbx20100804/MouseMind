# MouseMind Architecture

## Dependency direction

```text
MouseMind.Core
      ↑
MouseMind.Windows
      ↑
MouseMind.App

MouseMind.Tests → Core + Windows
```

- **Core** contains models, profile matching, action contracts, execution coordination and versioned configuration storage.
- **Windows** contains the dedicated Win32 hook thread, foreground-window lookup, shortcut parsing and keyboard injection.
- **App** is the WPF composition root and owns Dispatcher/UI updates and Prism motion.
- **Tests** exercise Core and injectable Windows logic without sending real keyboard input.

## Input pipeline

```text
Dedicated WH_MOUSE_LL thread
  → bounded Channel<MouseInputEvent>
  → background single consumer
  → foreground process resolution
  → UI profile snapshot/match
  → target-window freshness check
  → ActionExecutionService
  → platform executor
  → UI log and toast
```

The native hook callback performs only fixed-cost parsing, foreground handle capture and `TryWrite`. It never queries process objects, awaits tasks or accesses WPF controls.

## Action guarantees

- Per-action mutual exclusion.
- Explicit `Success`, `Failed`, `Skipped`, `Cancelled` and `TimedOut` outcomes.
- Per-action cooldown state with periodic stale-entry pruning.
- Linked cancellation and configurable timeout.
- Emergency key-up injection if a native keyboard send is only partially accepted.
- Injectable `IKeyboardInputSender` so CI tests never call `SendInput`.

## Configuration guarantees

Schema v1 uses a versioned document:

```json
{
  "schemaVersion": 1,
  "savedAtUtc": "2026-09-02T00:00:00Z",
  "profiles": []
}
```

- Legacy root-array documents are migrated.
- Future schema versions enable read-only protection.
- Saves are serialized with a semaphore.
- A detached snapshot is written to a unique same-directory temporary file.
- The temporary document is flushed and parsed again before commit.
- `File.Replace` atomically commits and retains the previous primary as `.bak`.
- A corrupt primary can be quarantined and restored from the verified backup.

## Shutdown order

1. Cancel the input consumer lifetime.
2. Post `WM_QUIT` to the dedicated hook thread.
3. Unhook on the hook thread.
4. Complete the input channel.
5. Release app resources and close the WPF window.

