import { DOCUMENT, Injectable, inject } from '@angular/core';
import type { editor } from 'monaco-editor/editor/editor.api.js';
import { ThemeService, type Theme } from '../theme/theme.service';
import type { Monaco } from './monaco-loader';

export const THEME_TOKENS = [
  'background',
  'foreground',
  'muted',
  'muted-foreground',
  'popover',
  'popover-foreground',
  'border',
  'ring',
  'accent-text',
  'success',
  'warning',
  'destructive',
] as const;

export type ThemeToken = (typeof THEME_TOKENS)[number];

/** Token values as `#rrggbb` hex, read from the CSS custom properties of one token set. */
export type ThemeTokenColours = Record<ThemeToken, string>;

const bases: Record<Theme, editor.BuiltinTheme> = {
  light: 'vs',
  dark: 'vs-dark',
  'hc-light': 'hc-light',
  'hc-dark': 'hc-black',
};

/** Builds a Monaco theme from the design tokens; syntax colours come from the matching built-in theme. */
export function buildMonacoTheme(theme: Theme, t: ThemeTokenColours): editor.IStandaloneThemeData {
  const highContrast = theme === 'hc-light' || theme === 'hc-dark';
  const colors: Record<string, string> = {
    'editor.background': t.background,
    'editor.foreground': t.foreground,
    'editorGutter.background': t.background,
    'editorLineNumber.foreground': t['muted-foreground'],
    'editorLineNumber.activeForeground': t.foreground,
    'editorCursor.foreground': t.foreground,
    'editor.lineHighlightBackground': highContrast ? t.background : t.muted,
    'editor.lineHighlightBorder': highContrast ? t.border : t.muted,
    'editor.selectionBackground': `${t.ring}4d`,
    'editor.inactiveSelectionBackground': `${t.ring}26`,
    'editor.findMatchHighlightBackground': `${t.warning}4d`,
    'editorIndentGuide.background1': t.border,
    'editorWhitespace.foreground': t.border,
    'editorWidget.background': t.popover,
    'editorWidget.foreground': t['popover-foreground'],
    'editorWidget.border': t.border,
    'editorHoverWidget.background': t.popover,
    'editorHoverWidget.foreground': t['popover-foreground'],
    'editorHoverWidget.border': t.border,
    'textLink.foreground': t['accent-text'],
    focusBorder: t.ring,
    'scrollbarSlider.background': `${t['muted-foreground']}33`,
    'scrollbarSlider.hoverBackground': `${t['muted-foreground']}55`,
    'scrollbarSlider.activeBackground': `${t['muted-foreground']}77`,
    'editorOverviewRuler.border': t.border,
    'diffEditor.border': t.border,
    'diffEditor.insertedTextBackground': `${t.success}40`,
    'diffEditor.removedTextBackground': `${t.destructive}40`,
    'diffEditor.insertedLineBackground': `${t.success}1f`,
    'diffEditor.removedLineBackground': `${t.destructive}1f`,
    'diffEditorGutter.insertedLineBackground': `${t.success}33`,
    'diffEditorGutter.removedLineBackground': `${t.destructive}33`,
    ...(highContrast ? { contrastBorder: t.border, contrastActiveBorder: t.ring } : {}),
  };
  return {
    base: bases[theme],
    inherit: true,
    rules: [],
    colors: Object.fromEntries(
      Object.entries(colors).filter(([, value]) => /^#[0-9a-f]{6}/i.test(value)),
    ),
  };
}

/** Defines the Monaco theme for the active token set and switches every editor on the page to it. */
@Injectable({ providedIn: 'root' })
export class MonacoTheme {
  private readonly document = inject(DOCUMENT);
  private readonly themeService = inject(ThemeService);
  private readonly defined = new Set<Theme>();

  readonly theme = this.themeService.theme;

  apply(monaco: Monaco, theme: Theme): void {
    const name = `hirelab-${theme}`;
    if (!this.defined.has(theme)) {
      monaco.editor.defineTheme(name, buildMonacoTheme(theme, this.readTokens(theme)));
      this.defined.add(theme);
    }
    monaco.editor.setTheme(name);
  }

  /** The code font from the `--font-mono` token. */
  fontFamily(): string {
    const value = this.document.defaultView
      ?.getComputedStyle(this.document.documentElement)
      .getPropertyValue('--font-mono')
      .trim();
    return value || "'JetBrains Mono', ui-monospace, monospace";
  }

  private readTokens(theme: Theme): ThemeTokenColours {
    const probe = this.document.createElement('div');
    probe.className = theme;
    probe.hidden = true;
    this.document.body.appendChild(probe);
    const style = this.document.defaultView?.getComputedStyle(probe);
    const canvas = this.document.createElement('canvas');
    canvas.width = 1;
    canvas.height = 1;
    const context = canvas.getContext('2d', { willReadFrequently: true });
    const colours = Object.fromEntries(
      THEME_TOKENS.map((token) => [
        token,
        toHex(context, style?.getPropertyValue(`--${token}`).trim() ?? ''),
      ]),
    ) as ThemeTokenColours;
    probe.remove();
    return colours;
  }
}

function toHex(context: CanvasRenderingContext2D | null, value: string): string {
  if (!context || !value) {
    return '';
  }
  context.clearRect(0, 0, 1, 1);
  context.fillStyle = value;
  context.fillRect(0, 0, 1, 1);
  const [r, g, b] = context.getImageData(0, 0, 1, 1).data;
  return `#${[r, g, b].map((channel) => channel.toString(16).padStart(2, '0')).join('')}`;
}
