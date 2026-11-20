import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  Directive,
  TemplateRef,
  computed,
  contentChild,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import {
  CodeDiff,
  CodeZones,
  type CodeKeyAction,
  type CodeKeyActionEvent,
  type CodeLineDecoration,
  type DiffLine,
  type DiffLineRange,
} from '@hirelab/ui/code';
import type { ChangedFile, ReviewComment } from './code-review';
import { ReviewCommentView } from './review-comment';
import { commentDecorations, reviewThreads, type ReviewThread } from './review-threads';

export interface ReviewThreadContext {
  readonly $implicit: ReviewThread;
}

/**
 * Replaces how an `hl-code-review-diff` renders a thread under its line:
 * `<ng-template hlReviewThread let-thread>…</ng-template>`. Without it, the comments are shown read-only.
 */
@Directive({ selector: 'ng-template[hlReviewThread]' })
export class ReviewThreadTemplate {
  readonly template = inject<TemplateRef<ReviewThreadContext>>(TemplateRef);

  static ngTemplateContextGuard(
    _directive: ReviewThreadTemplate,
    context: unknown,
  ): context is ReviewThreadContext {
    return typeof context === 'object';
  }
}

/**
 * One changed file's diff with its review comments: lines with comments are highlighted and the comments are shown
 * as threads under the line they end on. Extra decorations (such as expected findings) go on top.
 */
@Component({
  selector: 'hl-code-review-diff',
  imports: [CodeDiff, CodeZones, NgTemplateOutlet, ReviewCommentView],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <hl-code-diff
      class="h-full"
      [original]="file().original"
      [modified]="file().modified ?? ''"
      [language]="file().language"
      [renderSideBySide]="sideBySide()"
      [ariaLabel]="'Diff of ' + file().path"
      [decorations]="allDecorations()"
      [gutterButton]="gutterButton()"
      [keyActions]="keyActions()"
      (gutterClick)="gutterClick.emit($event)"
      (selectionChange)="selectionChange.emit($event)"
      (keyAction)="keyAction.emit($event)"
    >
      <ng-template [hlCodeZones]="threads()" let-zone>
        <div
          class="mx-2 my-1 flex flex-col gap-3 rounded-md border bg-card p-3 font-sans text-card-foreground"
        >
          @if (threadTemplate(); as custom) {
            <ng-container
              [ngTemplateOutlet]="custom.template"
              [ngTemplateOutletContext]="{ $implicit: zone.data }"
            />
          } @else {
            @for (comment of zone.data.comments; track comment.id) {
              <hl-review-comment [comment]="comment" />
            }
          }
        </div>
      </ng-template>
    </hl-code-diff>
  `,
})
export class CodeReviewDiff {
  private readonly diff = viewChild(CodeDiff);
  protected readonly threadTemplate = contentChild(ReviewThreadTemplate);

  readonly file = input.required<ChangedFile>();
  /** Review comments of any file; only the ones on `file` are shown. */
  readonly comments = input<readonly ReviewComment[]>([]);
  readonly sideBySide = input(false);
  /** Where a new comment is being written; its thread gets `draft: true`. */
  readonly draft = input<DiffLineRange | null>(null);
  readonly decorations = input<readonly CodeLineDecoration[]>([]);
  readonly gutterButton = input<string | null>(null);
  readonly keyActions = input<readonly CodeKeyAction[]>([]);

  readonly gutterClick = output<DiffLine>();
  readonly selectionChange = output<DiffLineRange>();
  readonly keyAction = output<CodeKeyActionEvent>();

  readonly loaded = computed(() => this.diff()?.loaded() ?? false);

  protected readonly fileComments = computed(() =>
    this.comments().filter((comment) => comment.file === this.file().path),
  );
  protected readonly threads = computed(() =>
    reviewThreads(this.file().path, this.fileComments(), this.draft()),
  );
  protected readonly allDecorations = computed(() => [
    ...commentDecorations(this.fileComments()),
    ...this.decorations(),
  ]);

  /** The line range of the current selection or cursor, or `null` before the diff has loaded. */
  getSelection(): DiffLineRange | null {
    return this.diff()?.getSelection() ?? null;
  }

  revealLine(line: DiffLine): void {
    this.diff()?.revealLine(line);
  }
}
