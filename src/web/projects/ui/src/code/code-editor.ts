import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterNextRender,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import type { IDisposable, editor } from 'monaco-editor/editor/editor.api.js';
import type { CodeLanguage } from './code-types';
import { baseEditorOptions, setModelValue } from './editor-options';
import { MonacoLoader, type Monaco } from './monaco-loader';
import { MonacoTheme } from './monaco-theme';

interface EditorState {
  readonly monaco: Monaco;
  readonly editor: editor.IStandaloneCodeEditor;
  readonly model: editor.ITextModel;
}

/** A Monaco editor for one file. Emits `valueChange` on every edit; Monaco loads lazily on first render. */
@Component({
  selector: 'hl-code-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'relative block min-h-48 overflow-hidden rounded-md border' },
  template: '<div #container class="absolute inset-0"></div>',
})
export class CodeEditor {
  private readonly loader = inject(MonacoLoader);
  private readonly monacoTheme = inject(MonacoTheme);
  private readonly container = viewChild.required<ElementRef<HTMLElement>>('container');

  readonly value = input('');
  readonly language = input<CodeLanguage>('plaintext');
  readonly readOnly = input(false);
  readonly ariaLabel = input('Code editor');

  readonly valueChange = output<string>();

  private readonly state = signal<EditorState | null>(null);
  private readonly disposables: IDisposable[] = [];
  private destroyed = false;

  /** True once Monaco has loaded and the editor is shown. */
  readonly loaded = computed(() => this.state() !== null);

  private readonly options = computed<editor.IEditorOptions>(() => ({
    readOnly: this.readOnly(),
    ariaLabel: this.ariaLabel(),
  }));

  constructor() {
    afterNextRender(() => void this.init());
    inject(DestroyRef).onDestroy(() => this.dispose());

    effect(() => {
      const state = this.state();
      const theme = this.monacoTheme.theme();
      if (state) {
        untracked(() => this.monacoTheme.apply(state.monaco, theme));
      }
    });
    effect(() => {
      const state = this.state();
      const value = this.value();
      if (state) {
        untracked(() => setModelValue(state.model, value));
      }
    });
    effect(() => {
      const state = this.state();
      const language = this.language();
      if (state) {
        untracked(() => state.monaco.editor.setModelLanguage(state.model, language));
      }
    });
    effect(() => {
      const state = this.state();
      const options = this.options();
      if (state) {
        untracked(() => state.editor.updateOptions(options));
      }
    });
  }

  /** Moves the keyboard focus into the editor. */
  focus(): void {
    this.state()?.editor.focus();
  }

  private async init(): Promise<void> {
    const monaco = await this.loader.load();
    if (this.destroyed) {
      return;
    }
    this.monacoTheme.apply(monaco, this.monacoTheme.theme());
    const model = monaco.editor.createModel(this.value(), this.language());
    const codeEditor = monaco.editor.create(this.container().nativeElement, {
      ...baseEditorOptions(this.monacoTheme.fontFamily()),
      ...this.options(),
      model,
    });
    this.disposables.push(
      model.onDidChangeContent(() => {
        const value = model.getValue();
        if (value !== untracked(this.value)) {
          this.valueChange.emit(value);
        }
      }),
    );
    this.state.set({ monaco, editor: codeEditor, model });
  }

  private dispose(): void {
    this.destroyed = true;
    this.disposables.forEach((disposable) => disposable.dispose());
    const state = this.state();
    if (state) {
      state.editor.dispose();
      state.model.dispose();
    }
  }
}
