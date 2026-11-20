import {
  ChangeDetectionStrategy,
  Component,
  Injector,
  afterNextRender,
  computed,
  inject,
  input,
  linkedSignal,
  signal,
  viewChild,
} from '@angular/core';
import type { TaskContent } from '@hirelab/tasks/framework';
import type { ReviewTaskComponent } from '@hirelab/tasks/review';
import { HlmBadge } from '@hirelab/ui/helm/badge';
import { HlmButton } from '@hirelab/ui/helm/button';
import { MarkdownViewer } from '@hirelab/ui/markdown';
import { ChangedFileList } from '../candidate/changed-file-list';
import type {
  ChangedFile,
  CodeReviewContent,
  CodeReviewResponse,
  CodeReviewStaffContent,
  ExpectedFinding,
} from '../candidate/code-review';
import { CodeReviewDiff } from '../candidate/code-review-diff';
import { PullRequestHeader } from '../candidate/pull-request-header';
import { ReviewCommentView } from '../candidate/review-comment';
import { isAnchored, lineRangeLabel } from '../candidate/review-threads';
import { verdictLabels } from '../candidate/verdict';
import { findingDecorations, severityBadgeVariants, sortFindings } from './expected-findings';

let nextId = 0;

@Component({
  selector: 'hl-code-review-review',
  imports: [
    HlmBadge,
    HlmButton,
    MarkdownViewer,
    ChangedFileList,
    CodeReviewDiff,
    PullRequestHeader,
    ReviewCommentView,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-col gap-6' },
  template: `
    <hl-pull-request-header
      [title]="content().candidate.title"
      [description]="content().candidate.description"
    />

    @if (!response()) {
      <p class="rounded-md border border-dashed p-3 text-muted-foreground">
        {{
          live()
            ? 'The candidate hasn’t started the review yet.'
            : 'The candidate didn’t review this pull request.'
        }}
      </p>
    }

    <div class="grid gap-4 lg:grid-cols-[18rem_minmax(0,1fr)]">
      <div class="flex min-w-0 flex-col gap-6">
        <hl-changed-file-list
          [files]="files()"
          [comments]="anchoredComments()"
          [(selected)]="selectedPath"
        />

        @if (showFindings()) {
          <section class="flex flex-col gap-2" [attr.aria-labelledby]="id + '-findings'">
            <h3 [id]="id + '-findings'" class="font-semibold">
              Expected findings ({{ findings().length }})
            </h3>
            @if (findings().length) {
              <ul class="flex flex-col gap-3">
                @for (finding of findings(); track finding.id) {
                  <li class="flex flex-col gap-1 rounded-md border p-2">
                    <button
                      type="button"
                      class="flex flex-col items-start gap-1 rounded-md text-left hover:underline focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none"
                      (click)="showFinding(finding)"
                    >
                      <span class="flex items-center gap-2">
                        <span hlmBadge [variant]="severityVariants[finding.severity]">
                          {{ finding.severity }}
                        </span>
                        <span class="font-medium">{{ finding.title }}</span>
                      </span>
                      <span class="font-mono text-xs text-muted-foreground">
                        {{ finding.file }}, {{ linesOf(finding) }}
                      </span>
                    </button>
                    <hl-markdown class="text-sm" [markdown]="finding.description" />
                  </li>
                }
              </ul>
            } @else {
              <p class="text-muted-foreground">This task has no expected findings.</p>
            }
          </section>
        }
      </div>

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
                [attr.aria-pressed]="showFindings()"
                (click)="findingsChoice.set(!showFindings())"
              >
                Show expected findings
              </button>
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
            </div>
          </div>
          @if (!comments().length) {
            <p class="text-sm text-muted-foreground">
              {{ live() ? 'No comments yet.' : 'No comments.' }}
            </p>
          }

          <hl-code-review-diff
            class="h-[60vh]"
            [file]="file"
            [comments]="anchoredComments()"
            [sideBySide]="sideBySide()"
            [decorations]="decorations()"
          />
        </section>
      }
    </div>

    @if (notAnchored().length) {
      <section class="flex flex-col gap-2" [attr.aria-labelledby]="id + '-not-anchored'">
        <h3 [id]="id + '-not-anchored'" class="font-semibold">
          Not anchored ({{ notAnchored().length }})
        </h3>
        <p class="text-sm text-muted-foreground">
          These comments are on files or lines that aren’t in the pull request.
        </p>
        <ul class="flex flex-col gap-2">
          @for (comment of notAnchored(); track comment.id) {
            <li class="flex flex-col gap-1 rounded-md border p-3">
              <span class="truncate font-mono text-sm font-medium" [title]="comment.file">
                {{ comment.file }}
              </span>
              <hl-review-comment [comment]="comment" />
            </li>
          }
        </ul>
      </section>
    }

    <section class="flex flex-col gap-2" [attr.aria-labelledby]="id + '-summary'">
      <div class="flex flex-wrap items-center gap-2">
        <h3 [id]="id + '-summary'" class="font-semibold">Review summary</h3>
        @if (verdict(); as verdict) {
          <span hlmBadge variant="secondary">Verdict: {{ verdict }}</span>
        } @else {
          <span class="text-sm text-muted-foreground">No verdict</span>
        }
      </div>
      @if (summary().trim()) {
        <hl-markdown class="rounded-md border p-3" [markdown]="summary()" />
      } @else {
        <p class="rounded-md border border-dashed p-3 text-muted-foreground">
          {{ live() ? 'No summary yet.' : 'No summary.' }}
        </p>
      }
    </section>

    <section>
      <h3 class="mb-2 font-semibold">Reference answer</h3>
      <hl-markdown class="rounded-md border p-3" [markdown]="referenceAnswer()" />
    </section>

    @if (interviewerNotes(); as notes) {
      <section>
        <h3 class="mb-2 font-semibold">Interviewer notes</h3>
        <hl-markdown [markdown]="notes" />
      </section>
    }
  `,
})
export class CodeReviewReview implements ReviewTaskComponent<
  CodeReviewContent,
  CodeReviewStaffContent,
  CodeReviewResponse
> {
  private readonly injector = inject(Injector);
  private readonly diff = viewChild(CodeReviewDiff);

  readonly content = input.required<TaskContent<CodeReviewContent, CodeReviewStaffContent>>();
  readonly response = input.required<CodeReviewResponse | null>();
  readonly referenceAnswer = input.required<string>();
  readonly interviewerNotes = input<string>();
  readonly live = input(false);

  protected readonly id = `hl-code-review-review-${nextId++}`;
  protected readonly severityVariants = severityBadgeVariants;

  protected readonly files = computed(() => this.content().candidate.files);
  protected readonly comments = computed(() => this.response()?.comments ?? []);
  protected readonly anchoredComments = computed(() =>
    this.comments().filter((comment) => isAnchored(comment, this.files())),
  );
  protected readonly notAnchored = computed(() =>
    this.comments().filter((comment) => !isAnchored(comment, this.files())),
  );
  protected readonly summary = computed(() => this.response()?.summary ?? '');
  protected readonly verdict = computed(() => {
    const verdict = this.response()?.verdict;
    return verdict ? verdictLabels[verdict] : null;
  });

  protected readonly selectedPath = linkedSignal<readonly ChangedFile[], string>({
    source: this.files,
    computation: (files, previous) =>
      previous && files.some((file) => file.path === previous.value)
        ? previous.value
        : (files[0]?.path ?? ''),
  });
  protected readonly selectedFile = computed(
    () => this.files().find((file) => file.path === this.selectedPath()) ?? null,
  );
  protected readonly sideBySide = signal(false);

  /** The interviewer's own choice; until they make one, the findings are hidden while the session is live. */
  protected readonly findingsChoice = signal<boolean | null>(null);
  protected readonly showFindings = computed(() => this.findingsChoice() ?? !this.live());
  protected readonly findings = computed(() =>
    sortFindings(this.content().staff?.expectedFindings ?? []),
  );
  protected readonly decorations = computed(() =>
    this.showFindings() ? findingDecorations(this.selectedPath(), this.findings()) : [],
  );

  protected linesOf(finding: ExpectedFinding): string {
    return lineRangeLabel(finding);
  }

  protected showFinding(finding: ExpectedFinding): void {
    if (!this.files().some((file) => file.path === finding.file)) {
      return;
    }
    this.selectedPath.set(finding.file);
    afterNextRender(
      () => this.diff()?.revealLine({ side: finding.side, line: finding.startLine }),
      { injector: this.injector },
    );
  }
}
