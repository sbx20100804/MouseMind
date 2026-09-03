# Changelog

## 0.3.0-alpha - In development

### Added
- Prism premium desktop shell with custom window chrome.
- Dashboard, profiles, activity and settings navigation.
- Persistent Obsidian Glass design tokens and reusable WPF controls.
- Session action counter and mirrored recent-activity summary.
- Context-signal orbit animation tied to real mouse input.
- Restrained page transitions and action-result toasts.
- Reduced-motion support through Windows animation preferences.
- Sprint 1 Core, Windows and Tests project boundaries.
- Dedicated Win32 hook thread with a bounded input channel.
- Typed action outcomes for success, skip, cancellation, timeout and failure.
- Versioned profile documents, migration diagnostics and backup recovery.
- Automated tests for profile matching, shortcut parsing, execution coordination and storage.
- Native Windows Desktop Acrylic with Mica Alt, Mica and solid fallbacks.
- Liquid Glass material tokens and a restrained Context Lens visual.
- Accessible names for title-bar controls and switches.

### Changed
- Profile storage now uses atomic temporary-file replacement.
- Monitoring state is available from both the header and sidebar.
- Process matching is now exact, supports `.exe` suffixes, and recognizes `*` as a global profile.
- Explicit profiles now take precedence over wildcard profiles.
- Keyboard injection now performs emergency key release after partial native sends.
- CI now runs the complete unit-test suite after every Release build.
- Replaced the neon Prism dashboard with a calmer translucent control surface.
- Reworked navigation, settings groups, profile rows, buttons and Toasts around one material hierarchy.
- Removed permanent decorative rotation and reduced input pulse amplitude.
- Toast dismissal now also works when Windows animations are disabled.

## 0.2.0-alpha - 2026-09-01

### Added
- Real keyboard shortcut execution using Windows `SendInput`.
- Action contracts, execution context and structured results.
- Shortcut parsing for common modifiers, letters, digits and function keys.
- Per-action cooldown protection.
- Multilingual Chinese and English project documentation.
- Windows build workflow for GitHub Actions.

### Changed
- Default Code Workspace profile now includes an executable `Ctrl+Z` mapping.
- Project package metadata and version updated for the public alpha.

## 0.1.0-alpha - 2026-09-01

- Initial WPF control center.
- Global side-button observation.
- Foreground process and profile matching.
- Local profile persistence and event log.
