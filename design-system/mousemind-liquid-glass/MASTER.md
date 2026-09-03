# MouseMind Liquid Glass Design System

## Direction

**Frosted Focus** — a calm, translucent control surface that keeps the current application and mouse context clear. The signature visual is one Context Lens that responds briefly to real input; everything else stays quiet.

## Tokens

| Role | Value |
|---|---|
| Window fallback | `#FF17181D` |
| Window tint | `#A40B0D11` |
| Sidebar glass | `#7015171D` |
| Content glass | `#731F222A` |
| Elevated glass | `#A6343741` |
| Glass edge | `#26FFFFFF` |
| Primary text | `#F5F5F7` |
| Secondary text | `#B5B5BA` |
| Tertiary text | `#85858C` |
| Action blue | `#006EDC` |
| Hover blue | `#2997FF` |
| Success | `#30D158` |
| Warning | `#FFD60A` |
| Error | `#FF453A` |

## Typography

- Display: Segoe UI Variable Display
- Body: Segoe UI Variable Text
- Diagnostics only: Cascadia Code
- Page title: 32px Semibold
- Section title: 17px Semibold
- Body: 13–14px
- Caption: 12px minimum

## Geometry

- Main material group: 18px
- Inset group: 13px
- Controls/navigation: 9–10px
- Window corners: native DWM

## Rules

1. Use one OS backdrop per window.
2. Do not apply blur effects to content containers.
3. Do not place glass on glass unless the child is a temporary surface.
4. Do not use color decoratively on multiple controls.
5. Do not use permanent glow, orbit or oscillation.
6. Every interactive control needs hover, press and keyboard-focus feedback.
7. Reduced motion must preserve timing logic, especially Toast dismissal.
8. The solid fallback must remain fully usable.

