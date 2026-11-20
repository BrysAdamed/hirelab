import { signal, type Provider } from '@angular/core';
import type { Theme } from '../../theme/theme.service';
import { MonacoLoader, type Monaco } from '../monaco-loader';
import { MonacoTheme } from '../monaco-theme';

type Listener<T> = (event: T) => void;

class FakeEmitter<T> {
  private readonly listeners = new Set<Listener<T>>();

  readonly event = (listener: Listener<T>) => {
    this.listeners.add(listener);
    return { dispose: () => this.listeners.delete(listener) };
  };

  fire(event: T): void {
    this.listeners.forEach((listener) => listener(event));
  }
}

export interface FakeSelection {
  startLineNumber: number;
  startColumn: number;
  endLineNumber: number;
  endColumn: number;
}

export interface FakeDecoration {
  range: FakeSelection;
  options: Record<string, unknown>;
}

export interface FakeZone {
  afterLineNumber: number;
  heightInPx?: number;
  domNode: HTMLElement;
}

export interface FakeOverlayWidget {
  getId(): string;
  getDomNode(): HTMLElement;
}

export interface FakeAction {
  id: string;
  label: string;
  keybindings?: number[];
  run: () => void;
}

export class FakeModel {
  readonly contentChanged = new FakeEmitter<void>();
  readonly onDidChangeContent = this.contentChanged.event;
  disposed = false;

  constructor(
    private value: string,
    public language: string,
  ) {}

  getValue(): string {
    return this.value;
  }

  setValue(value: string): void {
    this.value = value;
    this.contentChanged.fire();
  }

  getLineCount(): number {
    return this.value.split(/\r\n|\r|\n/).length;
  }

  dispose(): void {
    this.disposed = true;
  }
}

export class FakeDecorationsCollection {
  decorations: FakeDecoration[] = [];

  set(decorations: FakeDecoration[]): void {
    this.decorations = decorations;
  }
}

export class FakeCodeEditor {
  readonly mouseMove = new FakeEmitter<unknown>();
  readonly mouseDown = new FakeEmitter<unknown>();
  readonly mouseLeave = new FakeEmitter<unknown>();
  readonly cursorSelection = new FakeEmitter<{ selection: FakeSelection }>();
  readonly focusText = new FakeEmitter<void>();
  readonly onMouseMove = this.mouseMove.event;
  readonly onMouseDown = this.mouseDown.event;
  readonly onMouseLeave = this.mouseLeave.event;
  readonly onDidChangeCursorSelection = this.cursorSelection.event;
  readonly onDidFocusEditorText = this.focusText.event;
  readonly layoutChanged = new FakeEmitter<void>();
  readonly onDidLayoutChange = this.layoutChanged.event;
  readonly widgets: FakeOverlayWidget[] = [];

  readonly collections: FakeDecorationsCollection[] = [];
  readonly zones = new Map<string, FakeZone>();
  readonly actions: FakeAction[] = [];
  readonly revealed: number[] = [];
  options: Record<string, unknown> = {};
  selection: FakeSelection = { startLineNumber: 1, startColumn: 1, endLineNumber: 1, endColumn: 1 };
  focused = false;
  disposed = false;
  private zoneCount = 0;

  constructor(public model: FakeModel | null = null) {}

  /** The first collection holds the line decorations, the second the gutter button. */
  get decorations(): FakeDecoration[] {
    return this.collections[0]?.decorations ?? [];
  }

  get hover(): FakeDecoration[] {
    return this.collections[1]?.decorations ?? [];
  }

  createDecorationsCollection(): FakeDecorationsCollection {
    const collection = new FakeDecorationsCollection();
    this.collections.push(collection);
    return collection;
  }

  changeViewZones(
    callback: (accessor: {
      addZone: (zone: FakeZone) => string;
      removeZone: (id: string) => void;
      layoutZone: (id: string) => void;
    }) => void,
  ): void {
    callback({
      addZone: (zone) => {
        const id = `zone-${++this.zoneCount}`;
        this.zones.set(id, zone);
        return id;
      },
      removeZone: (id) => this.zones.delete(id),
      layoutZone: () => undefined,
    });
  }

  addOverlayWidget(widget: FakeOverlayWidget): void {
    this.widgets.push(widget);
  }

  removeOverlayWidget(widget: FakeOverlayWidget): void {
    this.widgets.splice(this.widgets.indexOf(widget), 1);
  }

  addAction(action: FakeAction) {
    this.actions.push(action);
    return { dispose: () => this.actions.splice(this.actions.indexOf(action), 1) };
  }

  getSelection(): FakeSelection {
    return this.selection;
  }

  hasTextFocus(): boolean {
    return this.focused;
  }

  select(selection: FakeSelection): void {
    this.selection = selection;
    this.focused = true;
    this.cursorSelection.fire({ selection });
  }

  revealLineInCenter(line: number): void {
    this.revealed.push(line);
  }

  updateOptions(options: Record<string, unknown>): void {
    this.options = { ...this.options, ...options };
  }

  getOption(): number {
    return 20;
  }

  getTopForLineNumber(line: number): number {
    return (line - 1) * 20;
  }

  getScrollTop(): number {
    return 0;
  }

  getLayoutInfo() {
    return { contentLeft: 50, contentWidth: 640, verticalScrollbarWidth: 14 };
  }

  getDomNode(): HTMLElement | null {
    return null;
  }

  focus(): void {
    this.focused = true;
  }

  dispose(): void {
    this.disposed = true;
  }
}

export class FakeDiffEditor {
  readonly original = new FakeCodeEditor();
  readonly modified = new FakeCodeEditor();
  readonly diffUpdated = new FakeEmitter<void>();
  readonly onDidUpdateDiff = this.diffUpdated.event;
  lineChanges: {
    originalStartLineNumber: number;
    originalEndLineNumber: number;
    modifiedStartLineNumber: number;
    modifiedEndLineNumber: number;
  }[] = [];
  models: { original: FakeModel; modified: FakeModel } | null = null;
  options: Record<string, unknown>;
  disposed = false;

  constructor(options: Record<string, unknown>) {
    this.options = options;
  }

  setModel(models: { original: FakeModel; modified: FakeModel }): void {
    this.models = models;
  }

  getOriginalEditor(): FakeCodeEditor {
    return this.original;
  }

  getModifiedEditor(): FakeCodeEditor {
    return this.modified;
  }

  getLineChanges() {
    return this.lineChanges;
  }

  updateOptions(options: Record<string, unknown>): void {
    this.options = { ...this.options, ...options };
  }

  dispose(): void {
    this.disposed = true;
  }
}

export const fakeMouseTargetType = {
  GUTTER_GLYPH_MARGIN: 2,
  GUTTER_LINE_NUMBERS: 3,
  GUTTER_LINE_DECORATIONS: 4,
  GUTTER_VIEW_ZONE: 5,
  CONTENT_TEXT: 6,
  CONTENT_EMPTY: 7,
  CONTENT_VIEW_ZONE: 8,
};

/** A hand-written stand-in for the parts of the Monaco API the wrappers use. */
export class FakeMonaco {
  readonly diffEditors: FakeDiffEditor[] = [];
  readonly codeEditors: (FakeCodeEditor & { createOptions: Record<string, unknown> })[] = [];
  readonly models: FakeModel[] = [];

  readonly KeyMod = { CtrlCmd: 2048, Shift: 1024, Alt: 512 };
  readonly KeyCode = { KeyM: 43, Digit1: 22, F2: 60 };

  readonly editor = {
    MouseTargetType: fakeMouseTargetType,
    OverviewRulerLane: { Left: 1 },
    EditorOption: { lineHeight: 75 },
    createDiffEditor: (_element: HTMLElement, options: Record<string, unknown>) => {
      const diff = new FakeDiffEditor(options);
      this.diffEditors.push(diff);
      return diff;
    },
    create: (_element: HTMLElement, options: Record<string, unknown> & { model: FakeModel }) => {
      const codeEditor = Object.assign(new FakeCodeEditor(options.model), {
        createOptions: options,
      });
      this.codeEditors.push(codeEditor);
      return codeEditor;
    },
    createModel: (value: string, language: string) => {
      const model = new FakeModel(value, language);
      this.models.push(model);
      return model;
    },
    setModelLanguage: (model: FakeModel, language: string) => {
      model.language = language;
    },
    defineTheme: () => undefined,
    setTheme: () => undefined,
  };

  get diff(): FakeDiffEditor {
    return this.diffEditors[0];
  }
}

export interface FakeMonacoSetup {
  readonly monaco: FakeMonaco;
  readonly theme: ReturnType<typeof signal<Theme>>;
  readonly appliedThemes: Theme[];
  readonly providers: Provider[];
}

/** Providers that replace the Monaco loader and theme with fakes. */
export function provideFakeMonaco(): FakeMonacoSetup {
  const monaco = new FakeMonaco();
  const theme = signal<Theme>('light');
  const appliedThemes: Theme[] = [];
  return {
    monaco,
    theme,
    appliedThemes,
    providers: [
      {
        provide: MonacoLoader,
        useValue: { load: () => Promise.resolve(monaco as unknown as Monaco) },
      },
      {
        provide: MonacoTheme,
        useValue: {
          theme,
          apply: (_monaco: unknown, applied: Theme) => appliedThemes.push(applied),
          fontFamily: () => 'monospace',
        },
      },
    ],
  };
}
