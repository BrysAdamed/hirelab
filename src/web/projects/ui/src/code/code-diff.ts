import {
  ChangeDetectionStrategy,
  Component,
  DOCUMENT,
  DestroyRef,
  ElementRef,
  type EmbeddedViewRef,
  afterNextRender,
  computed,
  contentChild,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
  viewChild,
  ViewContainerRef,
  ViewEncapsulation,
} from '@angular/core';
import type { IDisposable, editor } from 'monaco-editor/editor/editor.api.js';
import type {
  CodeDecorationKind,
  CodeKeyAction,
  CodeKeyActionEvent,
  CodeLanguage,
  CodeLineDecoration,
  CodeViewZone,
  DiffLine,
  DiffLineRange,
  DiffSide,
} from './code-types';
import { CodeZones } from './code-zones';
import { deletedLineAt, placeLine, selectionLines, type LineChange } from './diff-lines';
import { baseEditorOptions, setModelValue } from './editor-options';
import { parseKeybinding } from './keybinding';
import { MonacoLoader, type Monaco } from './monaco-loader';
import { MonacoTheme } from './monaco-theme';

interface DiffState {
  readonly monaco: Monaco;
  readonly diff: editor.IStandaloneDiffEditor;
  readonly models: Record<DiffSide, editor.ITextModel>;
  readonly editors: Record<DiffSide, editor.IStandaloneCodeEditor>;
  readonly decorations: Record<DiffSide, editor.IEditorDecorationsCollection>;
  readonly hover: Record<DiffSide, editor.IEditorDecorationsCollection>;
}

interface RenderedZone {
  readonly context: { $implicit: CodeViewZone };
  readonly view: EmbeddedViewRef<unknown>;
  readonly content: HTMLElement;
  readonly monacoZone: editor.IViewZone;
  readonly widget: editor.IOverlayWidget;
  editor: DiffSide;
  monacoId: string | null;
}

const sides: readonly DiffSide[] = ['original', 'modified'];

const decorationStyles: Record<
  CodeDecorationKind,
  { className: string; marker: string; ruler: string }
> = {
  comment: {
    className: 'hl-code-comment-line',
    marker: 'hl-code-comment-marker',
    ruler: 'editorOverviewRuler.infoForeground',
  },
  finding: {
    className: 'hl-code-finding-line',
    marker: 'hl-code-finding-marker',
    ruler: 'editorOverviewRuler.warningForeground',
  },
};

const gutterButtonClass = 'codicon codicon-add hl-code-gutter-button';
const zoneOrdinal = 10_000;

/**
 * A Monaco diff of one file, inline by default or side by side. It shows decorations on line ranges, renders
 * zones (an `ng-template[hlCodeZones]` child) under lines, offers an optional gutter button and keyboard actions,
 * and reports the line range the user selects. Monaco loads lazily on first render.
 */
@Component({
  selector: 'hl-code-diff',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'relative block min-h-48 overflow-hidden rounded-md border' },
  template: '<div #container class="absolute inset-0"></div>',
  encapsulation: ViewEncapsulation.None,
  styles: `
    .hl-code-comment-line {
      background-color: color-mix(in oklab, var(--primary) 15%, transparent);
    }
    .hl-code-finding-line {
      background-color: color-mix(in oklab, var(--warning) 25%, transparent);
    }
    .hl-code-comment-marker,
    .hl-code-finding-marker {
      margin-left: 3px;
      width: 3px !important;
    }
    .hl-code-comment-marker {
      background-color: var(--accent-text);
    }
    .hl-code-finding-marker {
      background-color: var(--warning);
    }
    .hl-code-gutter-button {
      cursor: pointer;
      color: var(--accent-text);
    }
  `,
})
export class CodeDiff {
  private readonly loader = inject(MonacoLoader);
  private readonly monacoTheme = inject(MonacoTheme);
  private readonly document = inject(DOCUMENT);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly container = viewChild.required<ElementRef<HTMLElement>>('container');
  private readonly zoneTemplate = contentChild(CodeZones);

  /** The file before the change; absent for an added file. */
  readonly original = input<string | null | undefined>(undefined);
  readonly modified = input.required<string>();
  readonly language = input<CodeLanguage>('plaintext');
  readonly renderSideBySide = input(false);
  readonly readOnly = input(true);
  readonly ariaLabel = input('Code diff');
  readonly decorations = input<readonly CodeLineDecoration[]>([]);
  readonly keyActions = input<readonly CodeKeyAction[]>([]);
  /** The hover text of the gutter button shown on the line under the mouse; `null` hides the button. */
  readonly gutterButton = input<string | null>(null);

  readonly gutterClick = output<DiffLine>();
  readonly selectionChange = output<DiffLineRange>();
  readonly keyAction = output<CodeKeyActionEvent>();

  private readonly state = signal<DiffState | null>(null);
  private readonly diffRevision = signal(0);
  private readonly hoveredLine = signal<{ editor: DiffSide; line: number } | null>(null);
  private readonly renderedZones = new Map<string, RenderedZone>();
  private readonly disposables: IDisposable[] = [];
  private readonly resizeObserver =
    typeof ResizeObserver === 'undefined'
      ? null
      : new ResizeObserver((entries) => this.onZonesResized(entries));
  private lastFocused: DiffSide = 'modified';
  private zoneCount = 0;
  private destroyed = false;

  /** True once Monaco has loaded and the diff is shown. */
  readonly loaded = computed(() => this.state() !== null);

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
      const original = this.original() ?? '';
      const modified = this.modified();
      if (state) {
        untracked(() => {
          setModelValue(state.models.original, original);
          setModelValue(state.models.modified, modified);
        });
      }
    });
    effect(() => {
      const state = this.state();
      const language = this.language();
      if (state) {
        untracked(() =>
          sides.forEach((side) =>
            state.monaco.editor.setModelLanguage(state.models[side], language),
          ),
        );
      }
    });
    effect(() => {
      const state = this.state();
      const options = this.diffOptions();
      if (state) {
        untracked(() => state.diff.updateOptions(options));
      }
    });
    effect(() => {
      const state = this.state();
      const decorations = this.decorations();
      const inline = !this.renderSideBySide();
      this.diffRevision();
      if (state) {
        untracked(() => this.renderDecorations(state, decorations, inline));
      }
    });
    effect(() => {
      const state = this.state();
      const template = this.zoneTemplate();
      const zones = template?.zones() ?? [];
      const inline = !this.renderSideBySide();
      this.diffRevision();
      if (state) {
        untracked(() => this.renderZones(state, template, zones, inline));
      }
    });
    effect(() => {
      const state = this.state();
      const hovered = this.hoveredLine();
      const label = this.gutterButton();
      if (state) {
        untracked(() =>
          sides.forEach((side) =>
            state.hover[side].set(
              hovered?.editor === side && label !== null
                ? [
                    {
                      range: lineRange(hovered.line, hovered.line),
                      options: {
                        glyphMarginClassName: gutterButtonClass,
                        glyphMarginHoverMessage: { value: label },
                      },
                    },
                  ]
                : [],
            ),
          ),
        );
      }
    });
    effect((onCleanup) => {
      const state = this.state();
      const actions = this.keyActions();
      if (!state) {
        return;
      }
      const handles = untracked(() => this.registerKeyActions(state, actions));
      onCleanup(() => handles.forEach((handle) => handle.dispose()));
    });
  }

  /** The line range of the current selection or cursor in the side the user last worked in. */
  getSelection(): DiffLineRange | null {
    const state = this.state();
    return state ? this.selectionOf(state, this.lastFocused) : null;
  }

  /** Scrolls a line into the middle of the view. */
  revealLine(target: DiffLine): void {
    const state = this.state();
    if (!state) {
      return;
    }
    const place = placeLine(
      target.side,
      target.line,
      !this.renderSideBySide(),
      this.lineChanges(state),
    );
    state.editors[place.editor].revealLineInCenter(place.line);
  }

  private readonly diffOptions = computed<editor.IDiffEditorOptions>(() => {
    const label = this.ariaLabel();
    return {
      renderSideBySide: this.renderSideBySide(),
      readOnly: this.readOnly(),
      glyphMargin: this.gutterButton() !== null,
      originalAriaLabel: `${label}, original`,
      modifiedAriaLabel: `${label}, modified`,
    };
  });

  private async init(): Promise<void> {
    const monaco = await this.loader.load();
    if (this.destroyed) {
      return;
    }
    this.monacoTheme.apply(monaco, this.monacoTheme.theme());
    const diff = monaco.editor.createDiffEditor(this.container().nativeElement, {
      ...baseEditorOptions(this.monacoTheme.fontFamily()),
      ...this.diffOptions(),
      originalEditable: false,
      useInlineViewWhenSpaceIsLimited: false,
      renderMarginRevertIcon: false,
      renderGutterMenu: false,
      ignoreTrimWhitespace: false,
    });
    const models = {
      original: monaco.editor.createModel(this.original() ?? '', this.language()),
      modified: monaco.editor.createModel(this.modified(), this.language()),
    };
    diff.setModel(models);
    const editors = { original: diff.getOriginalEditor(), modified: diff.getModifiedEditor() };
    const state: DiffState = {
      monaco,
      diff,
      models,
      editors,
      decorations: {
        original: editors.original.createDecorationsCollection(),
        modified: editors.modified.createDecorationsCollection(),
      },
      hover: {
        original: editors.original.createDecorationsCollection(),
        modified: editors.modified.createDecorationsCollection(),
      },
    };
    this.disposables.push(
      diff.onDidUpdateDiff(() => this.diffRevision.update((revision) => revision + 1)),
    );
    sides.forEach((side) => this.listen(state, side));
    this.state.set(state);
  }

  private listen(state: DiffState, side: DiffSide): void {
    const codeEditor = state.editors[side];
    const { MouseTargetType } = state.monaco.editor;
    this.disposables.push(
      codeEditor.onDidFocusEditorText(() => (this.lastFocused = side)),
      codeEditor.onDidLayoutChange(() => this.pinZones(state, side)),
      codeEditor.onDidChangeCursorSelection((event) => {
        if (codeEditor.hasTextFocus()) {
          this.lastFocused = side;
          this.selectionChange.emit(this.toRange(side, event.selection));
        }
      }),
      codeEditor.onMouseMove((event) => {
        const position = event.target.position;
        const overLine =
          this.gutterButton() !== null &&
          position !== null &&
          [
            MouseTargetType.GUTTER_GLYPH_MARGIN,
            MouseTargetType.GUTTER_LINE_NUMBERS,
            MouseTargetType.GUTTER_LINE_DECORATIONS,
            MouseTargetType.CONTENT_TEXT,
            MouseTargetType.CONTENT_EMPTY,
          ].includes(event.target.type);
        const hovered = this.hoveredLine();
        if (!overLine) {
          if (hovered?.editor === side) {
            this.hoveredLine.set(null);
          }
        } else if (hovered?.editor !== side || hovered.line !== position.lineNumber) {
          this.hoveredLine.set({ editor: side, line: position.lineNumber });
        }
      }),
      codeEditor.onMouseLeave(() => {
        if (this.hoveredLine()?.editor === side) {
          this.hoveredLine.set(null);
        }
      }),
      codeEditor.onMouseDown((event) => {
        if (this.gutterButton() === null) {
          return;
        }
        const target = event.target;
        if (target.type === MouseTargetType.GUTTER_GLYPH_MARGIN && target.position) {
          event.event.preventDefault();
          this.gutterClick.emit(this.toLine(side, target.position.lineNumber));
        } else if (
          target.type === MouseTargetType.GUTTER_VIEW_ZONE &&
          side === 'modified' &&
          !this.renderSideBySide()
        ) {
          const line = this.deletedLineUnder(
            state,
            target.detail,
            event.event.browserEvent.clientY,
          );
          if (line !== null) {
            event.event.preventDefault();
            this.gutterClick.emit({ side: 'original', line });
          }
        }
      }),
    );
  }

  private deletedLineUnder(
    state: DiffState,
    zone: editor.IMouseTargetViewZoneData,
    clientY: number,
  ): number | null {
    const ours = [...this.renderedZones.values()].some(
      (rendered) => rendered.monacoId === zone.viewZoneId,
    );
    if (ours) {
      return null;
    }
    const codeEditor = state.editors.modified;
    const lineHeight = codeEditor.getOption(state.monaco.editor.EditorOption.lineHeight);
    const zoneTop =
      zone.afterLineNumber === 0
        ? 0
        : codeEditor.getTopForLineNumber(zone.afterLineNumber) + lineHeight;
    const editorTop = codeEditor.getDomNode()?.getBoundingClientRect().top ?? 0;
    const y = clientY - editorTop + codeEditor.getScrollTop() - zoneTop;
    return deletedLineAt(zone.afterLineNumber, Math.floor(y / lineHeight), this.lineChanges(state));
  }

  private registerKeyActions(state: DiffState, actions: readonly CodeKeyAction[]): IDisposable[] {
    return actions.flatMap((action) =>
      sides.map((side) =>
        state.editors[side].addAction({
          id: `hirelab.${action.id}`,
          label: action.label,
          keybindings: [parseKeybinding(action.keys, state.monaco)],
          run: () => {
            const range = this.selectionOf(state, side);
            if (range) {
              this.keyAction.emit({ id: action.id, range });
            }
          },
        }),
      ),
    );
  }

  private renderDecorations(
    state: DiffState,
    decorations: readonly CodeLineDecoration[],
    inline: boolean,
  ): void {
    const changes = this.lineChanges(state);
    const bySide: Record<DiffSide, editor.IModelDeltaDecoration[]> = { original: [], modified: [] };
    for (const decoration of decorations) {
      const start = placeLine(decoration.side, decoration.startLine, inline, changes);
      const end = placeLine(decoration.side, decoration.endLine, inline, changes);
      const style = decorationStyles[decoration.kind];
      bySide[start.editor].push({
        range: lineRange(start.line, Math.max(start.line, end.line)),
        options: {
          isWholeLine: true,
          className: style.className,
          linesDecorationsClassName: style.marker,
          hoverMessage: decoration.hoverMessage ? { value: decoration.hoverMessage } : undefined,
          overviewRuler: {
            color: { id: style.ruler },
            position: state.monaco.editor.OverviewRulerLane.Left,
          },
        },
      });
    }
    sides.forEach((side) => state.decorations[side].set(bySide[side]));
  }

  private renderZones(
    state: DiffState,
    template: CodeZones<unknown> | undefined,
    zones: readonly CodeViewZone[],
    inline: boolean,
  ): void {
    const changes = this.lineChanges(state);
    const wanted = new Set(zones.map((zone) => zone.id));
    for (const [id, rendered] of this.renderedZones) {
      if (!wanted.has(id) || !template) {
        this.removeZone(state, id, rendered);
      }
    }
    if (!template) {
      return;
    }
    for (const zone of zones) {
      const place = placeLine(zone.side, zone.afterLine, inline, changes);
      const rendered = this.renderedZones.get(zone.id) ?? this.createZone(template, zone);
      if (rendered.context.$implicit !== zone) {
        rendered.context.$implicit = zone;
        rendered.view.detectChanges();
      }
      if (
        rendered.monacoId !== null &&
        rendered.editor === place.editor &&
        rendered.monacoZone.afterLineNumber === place.line
      ) {
        continue;
      }
      if (rendered.monacoId !== null) {
        this.detachZone(state, rendered);
      }
      rendered.editor = place.editor;
      rendered.monacoZone.afterLineNumber = place.line;
      const codeEditor = state.editors[place.editor];
      codeEditor.addOverlayWidget(rendered.widget);
      codeEditor.changeViewZones(
        (accessor) => (rendered.monacoId = accessor.addZone(rendered.monacoZone)),
      );
      this.pinZone(state, rendered);
    }
  }

  /** Keeps a zone's content over the code area, as wide as the visible code. */
  private pinZone(state: DiffState, rendered: RenderedZone): void {
    const layout = state.editors[rendered.editor].getLayoutInfo();
    rendered.content.style.left = `${layout.contentLeft}px`;
    rendered.content.style.width = `${Math.max(layout.contentWidth - layout.verticalScrollbarWidth, 0)}px`;
  }

  private pinZones(state: DiffState, side: DiffSide): void {
    for (const rendered of this.renderedZones.values()) {
      if (rendered.editor === side && rendered.monacoId !== null) {
        this.pinZone(state, rendered);
      }
    }
  }

  /**
   * A zone is an empty Monaco view zone that makes room under the line, plus an overlay widget with the content that
   * follows the view zone's position. Overlay widgets get their own mouse and keyboard input and don't scroll sideways
   * with the code.
   */
  private createZone(template: CodeZones<unknown>, zone: CodeViewZone): RenderedZone {
    const context = { $implicit: zone };
    const view = this.viewContainer.createEmbeddedView(template.template, context);
    const content = this.document.createElement('div');
    content.style.position = 'absolute';
    view.rootNodes.forEach((node: Node) => content.appendChild(node));
    for (const type of ['keydown', 'keyup', 'keypress', 'paste', 'cut', 'copy']) {
      content.addEventListener(type, (event) => event.stopPropagation());
    }
    view.detectChanges();
    const widgetId = `hirelab.zone.${++this.zoneCount}`;
    const rendered: RenderedZone = {
      context,
      view,
      content,
      widget: { getId: () => widgetId, getDomNode: () => content, getPosition: () => null },
      monacoZone: {
        afterLineNumber: zone.afterLine,
        heightInPx: Math.max(content.offsetHeight, 1),
        domNode: this.document.createElement('div'),
        ordinal: zoneOrdinal,
        onDomNodeTop: (top) => (content.style.top = `${top}px`),
      },
      editor: zone.side,
      monacoId: null,
    };
    this.renderedZones.set(zone.id, rendered);
    this.resizeObserver?.observe(content);
    return rendered;
  }

  private detachZone(state: DiffState, rendered: RenderedZone): void {
    const monacoId = rendered.monacoId;
    const codeEditor = state.editors[rendered.editor];
    if (monacoId !== null) {
      codeEditor.changeViewZones((accessor) => accessor.removeZone(monacoId));
    }
    codeEditor.removeOverlayWidget(rendered.widget);
    rendered.monacoId = null;
  }

  private removeZone(state: DiffState, id: string, rendered: RenderedZone): void {
    this.detachZone(state, rendered);
    this.resizeObserver?.unobserve(rendered.content);
    rendered.view.destroy();
    this.renderedZones.delete(id);
  }

  private onZonesResized(entries: readonly ResizeObserverEntry[]): void {
    const state = this.state();
    if (!state) {
      return;
    }
    for (const entry of entries) {
      const rendered = [...this.renderedZones.values()].find(
        (zone) => zone.content === entry.target,
      );
      const height = Math.max(rendered?.content.offsetHeight ?? 0, 1);
      if (rendered?.monacoId && rendered.monacoZone.heightInPx !== height) {
        const monacoId = rendered.monacoId;
        rendered.monacoZone.heightInPx = height;
        state.editors[rendered.editor].changeViewZones((accessor) => accessor.layoutZone(monacoId));
      }
    }
  }

  private selectionOf(state: DiffState, side: DiffSide): DiffLineRange | null {
    const selection = state.editors[side].getSelection();
    return selection ? this.toRange(side, selection) : null;
  }

  private toLine(editorSide: DiffSide, line: number): DiffLine {
    return { side: this.renderSideBySide() ? editorSide : 'modified', line };
  }

  private toRange(
    editorSide: DiffSide,
    selection: { startLineNumber: number; endLineNumber: number; endColumn: number },
  ): DiffLineRange {
    return {
      side: this.renderSideBySide() ? editorSide : 'modified',
      ...selectionLines(selection),
    };
  }

  private lineChanges(state: DiffState): readonly LineChange[] {
    return state.diff.getLineChanges() ?? [];
  }

  private dispose(): void {
    this.destroyed = true;
    this.resizeObserver?.disconnect();
    this.disposables.forEach((disposable) => disposable.dispose());
    const state = this.state();
    if (state) {
      state.diff.dispose();
      state.models.original.dispose();
      state.models.modified.dispose();
    }
  }
}

function lineRange(startLine: number, endLine: number) {
  return { startLineNumber: startLine, startColumn: 1, endLineNumber: endLine, endColumn: 1 };
}
