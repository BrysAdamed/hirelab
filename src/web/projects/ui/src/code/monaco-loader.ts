import { DOCUMENT, Injectable, inject } from '@angular/core';

export type Monaco = typeof import('monaco-editor/editor/editor.api.js');

/** The stylesheet each app builds from Monaco's CSS as a non-injected `styles` bundle named `monaco`. */
export const MONACO_STYLESHEET = 'monaco.css';

/**
 * Loads Monaco (with its workers and the ADR 0032 languages) and its stylesheet on first use, in lazy chunks.
 * Replace it in tests with a stub that resolves a fake API.
 */
@Injectable({ providedIn: 'root' })
export class MonacoLoader {
  private readonly document = inject(DOCUMENT);
  private monaco?: Promise<Monaco>;

  load(): Promise<Monaco> {
    this.monaco ??= Promise.all([
      import('./monaco/monaco').then((m) => m.monaco),
      this.loadStylesheet(),
    ]).then(([monaco]) => monaco);
    return this.monaco;
  }

  private loadStylesheet(): Promise<void> {
    return new Promise((resolve) => {
      const link = this.document.createElement('link');
      link.rel = 'stylesheet';
      link.href = MONACO_STYLESHEET;
      link.addEventListener('load', () => resolve());
      link.addEventListener('error', () => resolve());
      this.document.head.appendChild(link);
    });
  }
}
