# MouseMind Liquid Glass Design System

## Direction

**Frosted Focus** — a calm, translucent control surface that keeps the current application and mouse context clear. The signature visual is one Context Lens that responds briefly to real input; everything else stays quiet.

## Tokens

| Role | Value |
|---|---|
| Window fallback | `#FF17181D` |
| Window tint | `#8A0B0E14` |
| Sidebar glass | `#8A14171D` |
| Content glass | `#B31A1D24` |
| Elevated glass | `#DD2D3039` |
| Glass edge | `#18FFFFFF` |
| Primary text | `#F5F5F7` |
| Secondary text | `#C7C7CC` |
| Tertiary text | `#A1A1A6` |
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
- Caption: 13px / 18px line height

## Geometry

- Main material group: 20px
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
