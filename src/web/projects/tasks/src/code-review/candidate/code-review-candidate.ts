import { DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  linkedSignal,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideMessageSquarePlus, lucidePencil } from '@ng-icons/lucide';
import type { CandidateTaskComponent } from '@hirelab/tasks/candidate';
import type { CodeKeyAction, DiffLineRange } from '@hirelab/ui/code';
import { HlmButton } from '@hirelab/ui/helm/button';
import { ChangedFileList } from './changed-file-list';
import {
  codeReviewLimits,
  createEmptyCodeReviewResponse,
  type CodeReviewContent,
  type CodeReviewResponse,
  type ReviewComment,
} from './code-review';
import { CodeReviewDiff, ReviewThreadTemplate } from './code-review-diff';
import { DeleteReviewCommentAction } from './delete-review-comment-action';
import { FinishReviewPanel } from './finish-review-panel';
import { PullRequestHeader } from './pull-request-header';
import { ReviewCommentView } from './review-comment';
import { ReviewCommentForm } from './review-comment-form';
import { lineRangeLabel } from './review-threads';

interface Draft {
  readonly file: string;
  readonly range: DiffLineRange;
}

let nextId = 0;

const commentAction: CodeKeyAction = {
  id: 'comment',
  label: 'Add a review comment',
  keys: 'Ctrl+Alt+M',
};

@Component({
  selector: 'hl-code-review-candidate',
  imports: [
    DecimalPipe,
    NgIcon,
    HlmButton,
    ChangedFileList,
    CodeReviewDiff,
    DeleteReviewCommentAction,
    FinishReviewPanel,
    PullRequestHeader,
    ReviewCommentForm,
    ReviewCommentView,
    ReviewThreadTemplate,
  ],
  providers: [provideIcons({ lucideMessageSquarePlus, lucidePencil })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-col gap-6' },
  template: `
    <hl-pull-request-header [title]="content().title" [description]="content().description" />

    <div class="grid gap-4 lg:grid-cols-[18rem_minmax(0,1fr)]">
      <hl-changed-file-list
        [files]="content().files"
        [comments]="comments()"
        [(selected)]="selectedPath"
      />

      @if (selectedFile(); as file) {
        <section class="flex min-w-0 flex-col gap-2" [attr.aria-label]="'Review of ' + file.path">
          <div class="flex flex-wrap items-center justify-between gap-2">
            <h3 class="min-w-0 truncate font-mono font-semibold">{{ file.path }}</h3>
            <div class="flex items-center gap-2">
              <button
                hlmBtn
                type="button"
                variant="outline"
                size="sm"
                [attr.aria-pressed]="sideBySide()"
                (click)="sideBySide.set(!sideBySide())"
              >
                Side by side
              </button>
              <button
                hlmBtn
                type="button"
                size="sm"
                [disabled]="atLimit() || !reviewDiff.loaded()"
                [attr.aria-describedby]="hintId"
                (click)="commentOnSelection()"
              >
                <ng-icon name="lucideMessageSquarePlus" aria-hidden="true" />
                Comment
              </button>
            </div>
          </div>
          <p class="text-sm text-muted-foreground" [id]="hintId">
            @if (atLimit()) {
              You have written {{ maxComments | number }} comments, the most a review can have.
              Delete one to add another.
            } @else {
              Comment on the current line or selection with the Comment button,
              <kbd class="font-mono">Ctrl+Alt+M</kbd>, or the + next to a line.
              {{ comments().length | number }} of {{ maxComments | number }} comments.
            }
          </p>

          <hl-code-review-diff
            #reviewDiff
            class="h-[60vh]"
            [file]="file"
            [comments]="comments()"
            [sideBySide]="sideBySide()"
            [draft]="draftRange()"
            [gutterButton]="atLimit() ? null : 'Add a review comment'"
            [keyActions]="keyActions()"
            (gutterClick)="
              startComment({ side: $event.side, startLine: $event.line, endLine: $event.line })
            "
            (keyAction)="startComment($event.range)"
          >
            <ng-template hlReviewThread let-thread>
              @for (comment of thread.comments; track comment.id) {
                @if (editingId() === comment.id) {
                  <hl-review-comment-form
                    submitLabel="Save comment"
                    [range]="comment"
                    [initialBody]="comment.body"
                    (saved)="saveEdit(comment.id, $event)"
                    (cancelled)="editingId.set(null)"
                  />
                } @else {
                  <hl-review-comment [comment]="comment">
                    <button
                      hlmBtn
                      type="button"
                      variant="ghost"
                      size="xs"
                      [attr.aria-label]="'Edit comment on ' + linesOf(comment)"
                      (click)="edit(comment.id)"
                    >
                      <ng-icon name="lucidePencil" aria-hidden="true" />
                      Edit
                    </button>
                    <hl-delete-review-comment-action
                      [comment]="comment"
                      (confirmed)="deleteComment(comment.id)"
                    />
                  </hl-review-comment>
                }
              }
              @if (thread.draft && draftRange(); as range) {
                <hl-review-comment-form
                  [range]="range"
                  (saved)="addComment($event)"
                  (cancelled)="draft.set(null)"
                />
              }
            </ng-template>
          </hl-code-review-diff>
        </section>
      }
    </div>

    <hl-finish-review-panel
      [summary]="current().summary"
      [verdict]="current().verdict ?? null"
      (summaryChange)="update({ summary: $event })"
      (verdictChange)="update({ verdict: $event })"
    />
  `,
})
export class CodeReviewCandidate implements CandidateTaskComponent<
  CodeReviewContent,
  CodeReviewResponse | null
> {
  private readonly diff = viewChild(CodeReviewDiff);

  readonly content = input.required<CodeReviewContent>();
  readonly response = input.required<CodeReviewResponse | null>();
  readonly responseChange = output<CodeReviewResponse>();

  protected readonly hintId = `hl-code-review-hint-${nextId++}`;
  protected readonly maxComments = codeReviewLimits.maxComments;
  protected readonly current = computed(() => this.response() ?? createEmptyCodeReviewResponse());
  protected readonly comments = computed(() => this.current().comments);
  protected readonly atLimit = computed(() => this.comments().length >= this.maxComments);
  protected readonly keyActions = computed(() => (this.atLimit() ? [] : [commentAction]));

  protected readonly selectedPath = linkedSignal(() => this.content().files[0]?.path ?? '');
  protected readonly selectedFile = computed(
    () => this.content().files.find((file) => file.path === this.selectedPath()) ?? null,
  );
  protected readonly sideBySide = signal(false);
  protected readonly draft = signal<Draft | null>(null);
  protected readonly draftRange = computed(() => {
    const draft = this.draft();
    return draft && draft.file === this.selectedPath() ? draft.range : null;
  });
  protected readonly editingId = signal<string | null>(null);

  protected linesOf(comment: ReviewComment): string {
    return lineRangeLabel(comment);
  }

  protected commentOnSelection(): void {
    const range = this.diff()?.getSelection();
    if (range) {
      this.startComment(range);
    }
  }

  protected startComment(range: DiffLineRange): void {
    const file = this.selectedFile();
    if (!file || this.atLimit() || file[range.side] == null) {
      return;
    }
    this.editingId.set(null);
    this.draft.set({ file: file.path, range });
  }

  protected addComment(body: string): void {
    const draft = this.draft();
    if (!draft || this.atLimit()) {
      return;
    }
    const comment: ReviewComment = {
      id: crypto.randomUUID(),
      file: draft.file,
      side: draft.range.side,
      startLine: draft.range.startLine,
      endLine: draft.range.endLine,
      body,
    };
    this.draft.set(null);
    this.update({ comments: [...this.comments(), comment] });
  }

  protected edit(id: string): void {
    this.draft.set(null);
    this.editingId.set(id);
  }

  protected saveEdit(id: string, body: string): void {
    this.editingId.set(null);
    this.update({
      comments: this.comments().map((comment) =>
        comment.id === id ? { ...comment, body } : comment,
      ),
    });
  }

  protected deleteComment(id: string): void {
    if (this.editingId() === id) {
      this.editingId.set(null);
    }
    this.update({ comments: this.comments().filter((comment) => comment.id !== id) });
  }

  protected update(patch: Partial<CodeReviewResponse>): void {
    this.responseChange.emit({ ...this.current(), ...patch });
  }
}
