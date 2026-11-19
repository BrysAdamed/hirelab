import { ChangeDetectionStrategy, Component, signal, viewChild } from '@angular/core';
import { render } from '@testing-library/angular';
import type {
  CodeKeyAction,
  CodeKeyActionEvent,
  CodeLineDecoration,
  CodeViewZone,
  DiffLine,
  DiffLineRange,
} from './code-types';
import { CodeDiff } from './code-diff';
import { CodeZones } from './code-zones';
import { fakeMouseTargetType, provideFakeMonaco } from './testing/fake-monaco';

@Component({
  selector: 'hl-test-host',
  imports: [CodeDiff, CodeZones],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <hl-code-diff
      #diff
      language="csharp"
      [original]="original()"
      [modified]="modified()"
      [renderSideBySide]="sideBySide()"
      [decorations]="decorations()"
      [keyActions]="keyActions()"
      [gutterButton]="gutterButton()"
      (gutterClick)="clicks.push($event)"
      (selectionChange)="selections.push($event)"
      (keyAction)="actions.push($event)"
    >
      <ng-template [hlCodeZones]="zones()" let-zone>
        <p class="zone">{{ zone.data }}</p>
      </ng-template>
    </hl-code-diff>
  `,
})
class TestHost {
  readonly diff = viewChild.required<CodeDiff>('diff');
  readonly original = signal<string | undefined>('var a = 1;');
  readonly modified = signal('var a = 2;');
  readonly sideBySide = signal(false);
  readonly decorations = signal<readonly CodeLineDecoration[]>([]);
  readonly zones = signal<readonly CodeViewZone<string>[]>([]);
  readonly keyActions = signal<readonly CodeKeyAction[]>([]);
  readonly gutterButton = signal<string | null>(null);
  readonly clicks: DiffLine[] = [];
  readonly selections: DiffLineRange[] = [];
  readonly actions: CodeKeyActionEvent[] = [];
}

describe('CodeDiff', () => {
  async function setup() {
    const fake = provideFakeMonaco();
    const view = await render(TestHost, { providers: fake.providers });
    await vi.waitFor(() => expect(view.fixture.componentInstance.diff().loaded()).toBe(true));
    await view.fixture.whenStable();
    const host = view.fixture.componentInstance;
    const settle = async () => {
      view.fixture.detectChanges();
      await view.fixture.whenStable();
    };
    return { ...fake, host, settle, container: view.container, diff: fake.monaco.diff };
  }

  it('shows the original and modified file in the given language', async () => {
    // Act
    const { diff, appliedThemes } = await setup();

    // Assert
    expect(diff.models?.original.getValue()).toBe('var a = 1;');
    expect(diff.models?.modified.getValue()).toBe('var a = 2;');
    expect(diff.models?.modified.language).toBe('csharp');
    expect(diff.options['renderSideBySide']).toBe(false);
    expect(diff.options['originalEditable']).toBe(false);
    expect(appliedThemes).toContain('light');
  });

  it('shows an added file against an empty original and follows input changes', async () => {
    // Arrange
    const { host, diff, settle } = await setup();

    // Act
    host.original.set(undefined);
    host.modified.set('var b = 3;');
    host.sideBySide.set(true);
    await settle();

    // Assert
    expect(diff.models?.original.getValue()).toBe('');
    expect(diff.models?.modified.getValue()).toBe('var b = 3;');
    expect(diff.options['renderSideBySide']).toBe(true);
  });

  it('follows theme switches', async () => {
    // Arrange
    const { theme, appliedThemes, settle } = await setup();

    // Act
    theme.set('hc-dark');
    await settle();

    // Assert
    expect(appliedThemes.at(-1)).toBe('hc-dark');
  });

  it('puts decorations on their side when side by side', async () => {
    // Arrange
    const { host, diff, settle } = await setup();

    // Act
    host.sideBySide.set(true);
    host.decorations.set([
      { side: 'modified', startLine: 2, endLine: 3, kind: 'finding', hoverMessage: 'Fake finding' },
      { side: 'original', startLine: 1, endLine: 1, kind: 'comment' },
    ]);
    await settle();

    // Assert
    expect(diff.modified.decorations).toHaveLength(1);
    expect(diff.modified.decorations[0].range).toMatchObject({
      startLineNumber: 2,
      endLineNumber: 3,
    });
    expect(diff.modified.decorations[0].options).toMatchObject({
      isWholeLine: true,
      className: 'hl-code-finding-line',
      hoverMessage: { value: 'Fake finding' },
    });
    expect(diff.original.decorations).toHaveLength(1);
    expect(diff.original.decorations[0].options['className']).toBe('hl-code-comment-line');
  });

  it('maps original-side decorations onto the modified editor when inline', async () => {
    // Arrange
    const { host, diff, settle } = await setup();
    diff.lineChanges = [
      {
        originalStartLineNumber: 2,
        originalEndLineNumber: 0,
        modifiedStartLineNumber: 3,
        modifiedEndLineNumber: 5,
      },
    ];

    // Act
    host.decorations.set([{ side: 'original', startLine: 4, endLine: 4, kind: 'comment' }]);
    diff.diffUpdated.fire();
    await settle();

    // Assert
    expect(diff.original.decorations).toHaveLength(0);
    expect(diff.modified.decorations[0].range).toMatchObject({
      startLineNumber: 7,
      endLineNumber: 7,
    });
  });

  it('renders zones under their lines and keeps them in sync', async () => {
    // Arrange
    const { host, diff, settle } = await setup();

    // Act
    host.zones.set([{ id: 'thread-1', side: 'modified', afterLine: 1, data: 'Fake comment' }]);
    await settle();

    // Assert
    const [zone] = [...diff.modified.zones.values()];
    const content = () => diff.modified.widgets[0].getDomNode();
    expect(zone.afterLineNumber).toBe(1);
    expect(content().textContent).toContain('Fake comment');
    expect(content().style.left).toBe('50px');
    expect(content().style.width).toBe('626px');

    // Act
    host.zones.set([{ id: 'thread-1', side: 'modified', afterLine: 1, data: 'Edited comment' }]);
    await settle();

    // Assert
    expect(content().textContent).toContain('Edited comment');

    // Act
    host.zones.set([]);
    await settle();

    // Assert
    expect(diff.modified.zones.size).toBe(0);
    expect(diff.modified.widgets).toHaveLength(0);
  });

  it('emits the line of a gutter button click', async () => {
    // Arrange
    const { host, diff, settle } = await setup();
    host.gutterButton.set('Add a comment');
    await settle();

    // Act
    diff.modified.mouseMove.fire({
      target: { type: fakeMouseTargetType.CONTENT_TEXT, position: { lineNumber: 4 } },
    });
    await settle();
    diff.modified.mouseDown.fire({
      target: { type: fakeMouseTargetType.GUTTER_GLYPH_MARGIN, position: { lineNumber: 4 } },
      event: { preventDefault: () => undefined },
    });

    // Assert
    expect(diff.modified.hover[0].range.startLineNumber).toBe(4);
    expect(diff.modified.hover[0].options['glyphMarginHoverMessage']).toEqual({
      value: 'Add a comment',
    });
    expect(diff.options['glyphMargin']).toBe(true);
    expect(host.clicks).toEqual([{ side: 'modified', line: 4 }]);
  });

  it('ignores gutter clicks without a gutter button', async () => {
    // Arrange
    const { host, diff } = await setup();

    // Act
    diff.modified.mouseDown.fire({
      target: { type: fakeMouseTargetType.GUTTER_GLYPH_MARGIN, position: { lineNumber: 4 } },
      event: { preventDefault: () => undefined },
    });

    // Assert
    expect(host.clicks).toEqual([]);
  });

  it('emits the selected lines of the side the user selects in', async () => {
    // Arrange
    const { host, diff, settle } = await setup();
    host.sideBySide.set(true);
    await settle();

    // Act
    diff.original.select({ startLineNumber: 2, startColumn: 3, endLineNumber: 5, endColumn: 1 });

    // Assert
    expect(host.selections).toEqual([{ side: 'original', startLine: 2, endLine: 4 }]);
    expect(host.diff().getSelection()).toEqual({ side: 'original', startLine: 2, endLine: 4 });
  });

  it('runs a key action with the current line range', async () => {
    // Arrange
    const { host, diff, settle } = await setup();

    // Act
    host.keyActions.set([{ id: 'comment', label: 'Add a review comment', keys: 'Ctrl+Alt+M' }]);
    await settle();
    diff.modified.select({ startLineNumber: 3, startColumn: 1, endLineNumber: 3, endColumn: 8 });
    diff.modified.actions[0].run();

    // Assert
    expect(diff.modified.actions[0]).toMatchObject({
      label: 'Add a review comment',
      keybindings: [2048 | 512 | 43],
    });
    expect(host.actions).toEqual([
      { id: 'comment', range: { side: 'modified', startLine: 3, endLine: 3 } },
    ]);
  });

  it('reveals a line on its side', async () => {
    // Arrange
    const { host, diff } = await setup();

    // Act
    host.diff().revealLine({ side: 'modified', line: 6 });

    // Assert
    expect(diff.modified.revealed).toEqual([6]);
  });

  it('disposes the editor and models on destroy', async () => {
    // Arrange
    const fake = provideFakeMonaco();
    const view = await render(TestHost, { providers: fake.providers });
    await vi.waitFor(() => expect(fake.monaco.diffEditors).toHaveLength(1));

    // Act
    view.fixture.destroy();

    // Assert
    expect(fake.monaco.diff.disposed).toBe(true);
    expect(fake.monaco.models.every((model) => model.disposed)).toBe(true);
  });
});
