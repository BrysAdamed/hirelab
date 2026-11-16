# Design system

The rules and building blocks shared by the `staff` and `candidate` apps. The decisions behind them are in [ADR 0016](adr/0016-ui-stack.md). The living reference is the dev-only `/design` route in the `staff` app, which renders every component below in all four themes.

## Principles

- **A developer tool, not an HR product.** Quiet, neutral surfaces with one accent; information density over decoration. The code review task should feel like a real pull request review.
- **The candidate is under stress.** The candidate app is calmer: larger type, more space, fewer controls, and a prominent high-contrast switch.
- **Tokens only.** Components never use raw colours, sizes or fonts, only the tokens below. The `hirelab/no-raw-colours` lint rule (`src/web/eslint-rules/`) fails on hex values, colour functions, named CSS colours and Tailwind palette classes (`bg-red-500`, `text-white`) in component TypeScript, templates and styles; only `theme.css` may define colours.
- **Never colour alone.** Every state shown in colour also has an icon or a label.
- **Irreversible means confirmed.** Closing a position, finishing a session, publishing a task version and deleting all go through the confirm dialog.

## Themes

Four token sets. Theme (system / light / dark) and high contrast (system / on / off) are two independent switches, saved per browser.

| | Normal | High contrast |
|---|---|---|
| Light | `light` | `hc-light` |
| Dark | `dark` | `hc-dark` |

- Normal themes meet WCAG 2.2 AA contrast. High-contrast themes meet AAA (7:1).
- `npm run check:contrast` (in `src/web`) checks every text/background pair in all four themes and fails below the threshold. It also checks the focus ring (3:1), and in high contrast the input and border outlines (3:1).
- Monaco follows the same switches: `vs`, `vs-dark`, `hc-light`, `hc-black`.
- The tokens live in `projects/ui/src/styles/theme.css`. A token set is a class (`light`, `dark`, `hc-light`, `hc-dark`): `ThemeService` puts one on `<html>`, and any element (or a dialog's panel class) can carry another to show a subtree in a different theme, which is how `/design` shows all four at once. Tailwind's `dark:` variant follows the nearest token-set class.

## Colour tokens

Token names follow shadcn/ui so Spartan helm components work unchanged. Values are Tailwind palette steps; the high-contrast sets push them further apart until they reach 7:1.

| Token | Light | Dark | Use |
|---|---|---|---|
| `--background` | white | zinc-950 | Page background |
| `--foreground` | zinc-950 | zinc-50 | Body text |
| `--card` / `--card-foreground` | white / zinc-950 | zinc-900 / zinc-50 | Panels |
| `--popover` / `--popover-foreground` | white / zinc-950 | zinc-900 / zinc-50 | Menus, dialogs |
| `--muted` / `--muted-foreground` | zinc-100 / zinc-600 | zinc-800 / zinc-400 | Secondary surfaces and text |
| `--secondary` / `--secondary-foreground` | zinc-100 / zinc-900 | zinc-800 / zinc-50 | Secondary buttons and badges |
| `--accent` / `--accent-foreground` | zinc-100 / zinc-900 | zinc-800 / zinc-50 | Hover and selected surfaces (shadcn's `accent`; not the lime accent) |
| `--border` / `--input` | zinc-200 | zinc-800 | Borders, field outlines |
| `--primary` | lime-400 | lime-400 | Primary fill: buttons, active states |
| `--primary-foreground` | zinc-950 | zinc-950 | Text on a primary fill. Always dark. |
| `--accent-text` | lime-700 | lime-400 | Links and accent text |
| `--ring` | lime-600 | lime-400 | Focus ring |
| `--success` / `--success-foreground` | green-700 / white | green-500 / zinc-950 | Passed checks, Done |
| `--warning` / `--warning-foreground` | amber-500 / zinc-950 | amber-400 / zinc-950 | Reconnecting, drafts |
| `--destructive` / `--destructive-foreground` | red-600 / white | red-500 / zinc-950 | Failures, delete, critical checks |
| `--overlay` | black / 40% | black / 60% | Dialog backdrop |

High contrast: `hc-light` uses black text on white, zinc-800 muted text, zinc-700 borders, lime-900 accent text and ring, green-800 / red-800 fills with white text and amber-400 with black. `hc-dark` uses white on black, zinc-300 muted text and borders, lime-300 accent text and ring, and green-400 / amber-300 / red-300 fills with black text. `theme.css` is the source of truth.

Two adjustments from the plain shadcn values, both forced by the contrast check: success is green-700 in light (white on green-600 is 3.3:1), and destructive text in dark is zinc-950 (white on red-500 is 3.8:1). For the same reason the destructive button and badge are solid fills (`bg-destructive text-destructive-foreground`) rather than the generated tinted style, whose red text on a 10–30% red tint drops below 4.5:1, and the generated `link` variants use `text-accent-text` instead of `text-primary`.

**Lime rules:**
- Never put white text on lime.
- Never use `lime-400` or lighter for text on a light background.
- Lime and green sit close together, so success always carries a check icon.

## Typography

| Token | Value |
|---|---|
| UI font | Inter, self-hosted (`@fontsource/inter`) |
| Code font | JetBrains Mono, self-hosted (`@fontsource/jetbrains-mono`) |
| Base size | 14 px in `staff`, 16 px in `candidate` (body text; the rem scale stays at 16 px) |
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

Spartan helm components are generated with `npx ng g @spartan-ng/cli:ui <name>` (settings in `src/web/components.json`) into `projects/ui/src/helm/` and imported as `@hirelab/ui/helm/<name>`. Generated so far: button, input, label, card, badge, dialog, dropdown-menu, textarea (the textarea uses the input's field classes instead of the generated `spartan-textarea` style class, and exposes the `aria-describedby` input of `BrnFieldControlDescribedBy`, whose host binding otherwise overwrites an `[attr.aria-describedby]` binding). After regenerating, re-apply and review the local changes listed under Colour tokens.

| Component | Notes |
|---|---|
| App shell | Header with the theme and high-contrast switches and the user menu (a dropdown menu with the user's details, Change password and Log out), and a navigation sidebar built from the routes: a page declares its roles and nav item with `staffPage(...)` in `shell.routes.ts`. The candidate variant has no navigation. |
| Page header | Title, breadcrumbs, primary actions |
| Data table | Helm table + TanStack Table: sorting, filtering, pagination, empty state |
| Form fields | Input, textarea, select, checkbox, radio, switch, with label, hint and error text |
| Dialog | Generic modal |
| Confirm dialog | For irreversible actions. Names the action on the button ("Close position"), never just "OK". |
| Toast | Success, error, info. Errors stay until dismissed. |
| Empty state | Icon, one sentence, optional action |
| Status badge | Session (`Planned`, `Live`, `Finished`), session task (`Pending`, `Current`, `Done`), version (`Draft`, `vN`, `Active`), visibility (`Team`, `Global`, and `System` in the task bank). Always icon + label. |
| Markdown viewer | `hl-markdown` from `@hirelab/ui/markdown` (its own entry point): `marked` (GitHub flavoured) + DOMPurify. All user-authored markdown goes through it. |
| Markdown editor | Monaco in markdown mode with live preview |
| Code editor / diff editor | Monaco behind our wrapper, lazy-loaded, theme-synced |
| Rubric scorer | `scale` criteria as 0–4 segmented controls, `check` criteria as Yes / No, "not assessed", suggested values marked as such |
| Presence indicator | `hl-presence-indicator` from `@hirelab/ui/live`: candidate online (with the number of open connections when there is more than one), offline with when they were last seen, or not connected yet. Icon + label. |
| Reconnecting banner | `hl-reconnecting-banner` from `@hirelab/ui/live`: a warning strip, "Reconnecting…" plus what the user should know, in a polite live region that is always present. Shown while SignalR reconnects. |
| Copy-link button | `hl-copy-button` from `@hirelab/ui/copy` (its own entry point, so it stays out of the initial bundle). Copies the candidate, invite or reset link and confirms inline in a polite live region ("Copied"), or asks the user to copy by hand if the clipboard refuses. Switch the confirmation to a toast once toasts exist. |

New components are added here and to `/design` when a feature needs them.

## Accessibility checklist

- Every interactive element is reachable and usable by keyboard, with a visible focus ring (`--ring`).
- Every icon-only button has an accessible name.
- Dialogs trap focus and return it on close.
- Live regions announce toasts and the reconnecting banner.
- No information is conveyed by colour alone.
- Contrast: AA in normal themes, AAA in high-contrast themes, checked in CI.
