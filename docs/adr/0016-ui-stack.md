# 0016. UI stack

## Context

Two Angular apps share a `ui` lib ([0003](0003-repository-and-frontend-layout.md)). PrimeNG is out because of its paid licence. The code review task should feel like a real pull request review, and the live-coding and authoring screens need a real code editor ([0013](0013-task-module-contract.md), [0015](0015-task-authoring-in-the-app.md)). Candidates are under stress and may need high contrast. Task content is authored by users now, so whatever renders it must be safe.

We considered Angular Material 3 + CDK (safest, but a distinctly Google look), Spartan UI (shadcn/ui for Angular, Tailwind-native, younger), Taiga UI (complete, heavy) and CDK + Tailwind alone (all hand-built).

## Decision

**Components: Spartan UI.**
- Brain packages (headless, built on the Angular CDK) are pinned to exact versions.
- Helm components are generated with `@spartan-ng/cli` into the `ui` lib (`projects/ui/src/helm/…`) and become our code. Upgrading means regenerating them and reviewing the diff.
- The Angular CDK is used directly where Spartan has no primitive, for example drag-and-drop in the task queue.

**Styling: Tailwind v4** with CSS-first configuration, as the whole styling layer.
- The theme is CSS variables in the shadcn naming (`--background`, `--foreground`, `--primary`, `--muted`, `--destructive`, `--ring` …).
- Components use tokens only, never raw colour values. A lint rule enforces this.

**Look and feel:** a developer tool. Neutral zinc greys, one lime accent, dense staff screens, monospace touches, subtle borders instead of shadows, a small radius. The candidate app is calmer and roomier.

**Themes.** Four token sets:

| | Normal | High contrast (WCAG AAA, 7:1) |
|---|---|---|
| Light | `light` | `hc-light` |
| Dark | `dark` | `hc-dark` |

- Theme follows the system by default, or is set manually to light or dark.
- High contrast is a separate switch that follows `prefers-contrast: more` by default. It is offered in both apps and placed prominently in the candidate app.
- Choices are saved per browser (`localStorage`).
- Monaco follows the same switches with its built-in `vs`, `vs-dark`, `hc-light` and `hc-black` themes.

**Accent: lime.**
- `lime-400` is the primary fill, always with dark text on top.
- Text and links use `lime-700` in light mode and `lime-400` in dark mode. High contrast goes further until it reaches 7:1.
- A contrast-check script in CI validates every text/background token pair in all four themes.

**Typography.** Inter for the UI and JetBrains Mono for code, both self-hosted through `@fontsource`; no Google Fonts CDN. Base size 14 px in the staff app, 16 px in the candidate app.

**Icons.** Lucide through `@ng-icons/lucide`, as tree-shaken SVG.

**Tables.** Spartan's helm table with TanStack Table for sorting, filtering and pagination, client-side.

**Code editing.** Monaco (editor and diff editor), lazy-loaded, behind our own thin wrapper in the `ui` lib. No community wrapper package.

**Markdown.**
- Rendering: our own component on `marked` plus DOMPurify, so user-authored content is always sanitised.
- Editing: Monaco in markdown mode with a live preview.

**Screen sizes.** Desktop-first. Below about 1024 px the candidate app asks for a desktop or laptop. The staff app works down to tablet width.

**Accessibility.** WCAG 2.2 AA everywhere, plus AAA contrast in the high-contrast themes:
- keyboard navigation and visible focus
- nothing conveyed by colour alone; success, failure and state badges always carry an icon
- no motion under `prefers-reduced-motion`

**Design process.** Code-first: tokens and components in the `ui` lib, with `docs/design-system.md` and a dev-only `/design` route in the `staff` app as the living reference. Claude artifacts may be used to explore a screen. No Figma.

## Consequences

- The look fits a developer tool without fighting a library's defaults. One styling system, Tailwind, covers everything.
- Helm components are our code. We own their bugs and upgrades, and Spartan's smaller community means fewer ready answers. Pinned versions and regenerate-and-diff upgrades keep that manageable.
- Four themes cost four token sets and a contrast check. In exchange, high contrast is a first-class feature, not an afterthought.
- Lime constrains how the accent may be used (fills with dark text; darker lime for text). The contrast check catches mistakes.
- Monaco adds about 2–3 MB, loaded only on screens that need an editor.
- Self-hosted fonts and icons mean no third-party requests from candidates' browsers. PostHog (HLB-11) remains the only one.

## Status

Accepted
