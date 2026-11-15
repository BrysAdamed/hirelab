# Design system

The rules and building blocks shared by the `staff` and `candidate` apps. The decisions behind them are in [ADR 0016](adr/0016-ui-stack.md). The living reference is the dev-only `/design` route in the `staff` app, which renders every component below in all four themes.

## Principles

- **A developer tool, not an HR product.** Quiet, neutral surfaces with one accent; information density over decoration. The code review task should feel like a real pull request review.
- **The candidate is under stress.** The candidate app is calmer: larger type, more space, fewer controls, and a prominent high-contrast switch.
- **Tokens only.** Components never use raw colours, sizes or fonts, only the tokens below. A lint rule enforces this.
- **Never colour alone.** Every state shown in colour also has an icon or a label.
- **Irreversible means confirmed.** Closing a position, finishing a session, publishing a task version and deleting all go through the confirm dialog.

## Themes

Four token sets. Theme (system / light / dark) and high contrast (system / on / off) are two independent switches, saved per browser.

| | Normal | High contrast |
|---|---|---|
| Light | `light` | `hc-light` |
| Dark | `dark` | `hc-dark` |

- Normal themes meet WCAG 2.2 AA contrast. High-contrast themes meet AAA (7:1).
- A CI script checks every text/background pair in all four themes.
- Monaco follows the same switches: `vs`, `vs-dark`, `hc-light`, `hc-black`.

## Colour tokens

Token names follow shadcn/ui so Spartan helm components work unchanged. Values are Tailwind palette steps; the high-contrast sets push them further apart until they reach 7:1.

| Token | Light | Dark | Use |
|---|---|---|---|
| `--background` | white | zinc-950 | Page background |
| `--foreground` | zinc-950 | zinc-50 | Body text |
| `--card` / `--card-foreground` | white / zinc-950 | zinc-900 / zinc-50 | Panels |
| `--popover` / `--popover-foreground` | white / zinc-950 | zinc-900 / zinc-50 | Menus, dialogs |
| `--muted` / `--muted-foreground` | zinc-100 / zinc-600 | zinc-800 / zinc-400 | Secondary surfaces and text |
| `--border` / `--input` | zinc-200 | zinc-800 | Borders, field outlines |
| `--primary` | lime-400 | lime-400 | Primary fill: buttons, active states |
| `--primary-foreground` | zinc-950 | zinc-950 | Text on a primary fill. Always dark. |
| `--accent-text` | lime-700 | lime-400 | Links and accent text |
| `--ring` | lime-600 | lime-400 | Focus ring |
| `--success` / `--success-foreground` | green-600 / white | green-500 / zinc-950 | Passed checks, Done |
| `--warning` / `--warning-foreground` | amber-500 / zinc-950 | amber-400 / zinc-950 | Reconnecting, drafts |
| `--destructive` / `--destructive-foreground` | red-600 / white | red-500 / white | Failures, delete, critical checks |

**Lime rules:**
- Never put white text on lime.
- Never use `lime-400` or lighter for text on a light background.
- Lime and green sit close together, so success always carries a check icon.

## Typography

| Token | Value |
|---|---|
| UI font | Inter, self-hosted (`@fontsource/inter`) |
| Code font | JetBrains Mono, self-hosted (`@fontsource/jetbrains-mono`) |
| Base size | 14 px in `staff`, 16 px in `candidate` |
| Scale | Tailwind's `text-xs` … `text-2xl`; headings stop at `text-2xl` |
| Weights | 400 body, 500 labels and buttons, 600 headings |

Code, ids, version labels (`v3`) and keyboard shortcuts use the code font.

## Spacing, shape and motion

- **Spacing:** Tailwind's 4 px scale. Staff screens default to `gap-2` / `p-3`, the candidate app to `gap-4` / `p-6`.
- **Radius:** 6 px (`--radius: 0.375rem`); pills (fully rounded) only for badges.
- **Elevation:** a `--border` hairline instead of shadows. Overlays (dialogs, menus) get one soft shadow.
- **Motion:** 150 ms ease-out for overlays and toasts only. None under `prefers-reduced-motion`.

## Icons

Lucide through `@ng-icons/lucide`, 16 px in dense staff UI and 20 px in the candidate app. Icons next to text inherit its colour.

## Screen sizes

- `candidate`: at least about 1024 px. Below that it shows "Please use a desktop or laptop".
- `staff`: desktop-first, usable down to tablet width (768 px).

## Building blocks

All of these live in the `ui` lib and appear on `/design`.

| Component | Notes |
|---|---|
| App shell | Navigation, header, theme and high-contrast switches. The candidate variant has no navigation. |
| Page header | Title, breadcrumbs, primary actions |
| Data table | Helm table + TanStack Table: sorting, filtering, pagination, empty state |
| Form fields | Input, textarea, select, checkbox, radio, switch, with label, hint and error text |
| Dialog | Generic modal |
| Confirm dialog | For irreversible actions. Names the action on the button ("Close position"), never just "OK". |
| Toast | Success, error, info. Errors stay until dismissed. |
| Empty state | Icon, one sentence, optional action |
| Status badge | Session (`Planned`, `Live`, `Finished`), session task (`Pending`, `Current`, `Done`), version (`Draft`, `vN`, `Active`), visibility (`Team`, `Global`). Always icon + label. |
| Markdown viewer | `marked` + DOMPurify. All user-authored markdown goes through it. |
| Markdown editor | Monaco in markdown mode with live preview |
| Code editor / diff editor | Monaco behind our wrapper, lazy-loaded, theme-synced |
| Rubric scorer | `scale` criteria as 0–4 segmented controls, `check` criteria as Yes / No, "not assessed", suggested values marked as such |
| Presence indicator | Candidate online or offline and last seen, plus the number of open connections |
| Reconnecting banner | Shown while SignalR reconnects |
| Copy-link button | Copies the candidate or invite link and confirms with a toast |

New components are added here and to `/design` when a feature needs them.

## Accessibility checklist

- Every interactive element is reachable and usable by keyboard, with a visible focus ring (`--ring`).
- Every icon-only button has an accessible name.
- Dialogs trap focus and return it on close.
- Live regions announce toasts and the reconnecting banner.
- No information is conveyed by colour alone.
- Contrast: AA in normal themes, AAA in high-contrast themes, checked in CI.
