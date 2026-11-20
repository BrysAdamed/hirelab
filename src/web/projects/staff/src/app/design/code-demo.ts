import { ChangeDetectionStrategy, Component, computed, signal, viewChild } from '@angular/core';
import {
  CODE_LANGUAGES,
  CodeDiff,
  CodeEditor,
  CodeZones,
  type CodeKeyAction,
  type CodeLanguage,
  type CodeLineDecoration,
  type CodeViewZone,
  type DiffLineRange,
} from '@hirelab/ui/code';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmLabel } from '@hirelab/ui/helm/label';

interface DemoComment extends DiffLineRange {
  readonly id: string;
  readonly body: string;
}

const original = [
  'public sealed class DonationHandler(IDonationStore store)',
  '{',
  '    public async Task Handle(CreateDonation command)',
  '    {',
  '        var donation = new Donation(command.Amount);',
  '        store.Add(donation);',
  '    }',
  '}',
].join('\n');

const modified = [
  'public sealed class DonationHandler(IDonationStore store, TimeProvider clock)',
  '{',
  '    public async Task Handle(CreateDonation command)',
  '    {',
  '        var donation = new Donation(command.Amount, DateTime.Now);',
  '        store.Add(donation);',
  '        await store.SaveChangesAsync();',
  '    }',
  '}',
].join('\n');

@Component({
  selector: 'hl-code-demo',
  imports: [CodeDiff, CodeEditor, CodeZones, HlmButton, HlmLabel],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col gap-3">
      <div class="flex flex-wrap items-center gap-2">
        <button
          hlmBtn
          type="button"
          variant="outline"
          size="sm"
          (click)="sideBySide.set(!sideBySide())"
        >
          {{ sideBySide() ? 'Inline' : 'Side by side' }}
        </button>
        <button hlmBtn type="button" variant="outline" size="sm" (click)="commentOnSelection()">
          Comment
        </button>
        <span class="text-muted-foreground">
          Selection: <code>{{ selectionLabel() }}</code>. Add a comment from the gutter, the button
          or <kbd>Ctrl+Alt+M</kbd>.
        </span>
      </div>
      <hl-code-diff
        #diff
        class="h-80"
        language="csharp"
        ariaLabel="DonationHandler.cs"
        gutterButton="Add a comment"
        [original]="original"
        [modified]="modified"
        [renderSideBySide]="sideBySide()"
        [decorations]="decorations()"
        [keyActions]="keyActions"
        (gutterClick)="
          addComment({ side: $event.side, startLine: $event.line, endLine: $event.line })
        "
        (selectionChange)="selection.set($event)"
        (keyAction)="addComment($event.range)"
      >
        <ng-template [hlCodeZones]="zones()" let-zone>
          <div
            class="bg-card text-card-foreground mx-2 my-1 flex flex-col gap-1 rounded-md border p-2 font-sans"
          >
            @for (comment of zone.data; track comment.id) {
              <div class="flex items-start justify-between gap-2">
                <p>
                  <span class="text-muted-foreground font-mono text-xs">
                    {{ comment.side }} {{ comment.startLine }}–{{ comment.endLine }}
                  </span>
                  {{ comment.body }}
                </p>
                <button
                  hlmBtn
                  type="button"
                  variant="ghost"
                  size="xs"
                  (click)="removeComment(comment.id)"
                >
                  Delete
                </button>
              </div>
            }
          </div>
        </ng-template>
      </hl-code-diff>

      <div class="flex flex-col gap-1.5">
        <label hlmLabel for="code-demo-language">Editor language</label>
        <select
          id="code-demo-language"
          class="border-input bg-background h-9 w-48 rounded-md border px-2"
          (change)="language.set($any($event.target).value)"
        >
          @for (option of languages; track option) {
            <option [value]="option" [selected]="option === language()">{{ option }}</option>
          }
        </select>
      </div>
      <hl-code-editor
        class="h-48"
        ariaLabel="Example editor"
        [language]="language()"
        [value]="editorValue()"
        (valueChange)="editorValue.set($event)"
      />
      <p class="text-muted-foreground">{{ editorValue().length }} characters.</p>
    </div>
  `,
})
export class CodeDemo {
  private readonly diff = viewChild.required<CodeDiff>('diff');
  private nextId = 1;

  protected readonly original = original;
  protected readonly modified = modified;
  protected readonly languages = CODE_LANGUAGES;
  protected readonly keyActions: readonly CodeKeyAction[] = [
    { id: 'comment', label: 'Add a review comment', keys: 'Ctrl+Alt+M' },
  ];

  protected readonly sideBySide = signal(false);
  protected readonly selection = signal<DiffLineRange | null>(null);
  protected readonly language = signal<CodeLanguage>('typescript');
  protected readonly editorValue = signal('export const answer = 42;\n');
  protected readonly comments = signal<readonly DemoComment[]>([
    { id: 'c0', side: 'modified', startLine: 5, endLine: 5, body: 'Use the injected clock.' },
  ]);

  protected readonly selectionLabel = computed(() => {
    const selection = this.selection();
    return selection ? `${selection.side} ${selection.startLine}–${selection.endLine}` : 'none';
  });

  protected readonly decorations = computed<readonly CodeLineDecoration[]>(() => [
    {
      side: 'modified',
      startLine: 7,
      endLine: 7,
      kind: 'finding',
      hoverMessage: '**Major**: the handler saves without a unit of work.',
    },
    ...this.comments().map((comment) => ({ ...comment, kind: 'comment' as const })),
  ]);

  protected readonly zones = computed<readonly CodeViewZone<readonly DemoComment[]>[]>(() => {
    const byLine = new Map<string, DemoComment[]>();
    for (const comment of this.comments()) {
      const key = `${comment.side}:${comment.endLine}`;
      byLine.set(key, [...(byLine.get(key) ?? []), comment]);
    }
    return [...byLine].map(([id, comments]) => ({
      id,
      side: comments[0].side,
      afterLine: comments[0].endLine,
      data: comments,
    }));
  });

  protected commentOnSelection(): void {
    const range = this.diff().getSelection();
    if (range) {
      this.addComment(range);
    }
  }

  protected addComment(range: DiffLineRange): void {
    const id = `c${this.nextId++}`;
    this.comments.update((comments) => [
      ...comments,
      { id, ...range, body: `Made-up comment ${id}.` },
    ]);
  }

  protected removeComment(id: string): void {
    this.comments.update((comments) => comments.filter((comment) => comment.id !== id));
  }
}
