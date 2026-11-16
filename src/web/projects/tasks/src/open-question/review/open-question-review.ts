import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import type { TaskContent } from '@hirelab/tasks/framework';
import type { ReviewTaskComponent } from '@hirelab/tasks/review';
import { MarkdownViewer } from '@hirelab/ui/markdown';
import type {
  OpenQuestionContent,
  OpenQuestionResponse,
  OpenQuestionStaffContent,
} from '../candidate/open-question';

@Component({
  selector: 'hl-open-question-review',
  imports: [MarkdownViewer],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-col gap-4' },
  template: `
    <section>
      <h3 class="mb-2 font-semibold">Question</h3>
      <hl-markdown [markdown]="content().candidate.prompt" />
    </section>

    <div class="grid gap-4 lg:grid-cols-2">
      <section>
        <h3 class="mb-2 font-semibold">Answer</h3>
        @if (answer().trim()) {
          <hl-markdown class="rounded-md border p-3" [markdown]="answer()" />
        } @else {
          <p class="rounded-md border border-dashed p-3 text-muted-foreground">
            {{ live() ? 'The candidate hasn’t written anything yet.' : 'No answer.' }}
          </p>
        }
      </section>

      <section>
        <h3 class="mb-2 font-semibold">Reference answer</h3>
        <hl-markdown class="rounded-md border p-3" [markdown]="referenceAnswer()" />
      </section>
    </div>

    @if (interviewerNotes(); as notes) {
      <section>
        <h3 class="mb-2 font-semibold">Interviewer notes</h3>
        <hl-markdown [markdown]="notes" />
      </section>
    }
  `,
})
export class OpenQuestionReview implements ReviewTaskComponent<
  OpenQuestionContent,
  OpenQuestionStaffContent,
  OpenQuestionResponse
> {
  readonly content = input.required<TaskContent<OpenQuestionContent, OpenQuestionStaffContent>>();
  readonly response = input.required<OpenQuestionResponse | null>();
  readonly referenceAnswer = input.required<string>();
  readonly interviewerNotes = input<string>();
  readonly live = input(false);

  protected readonly answer = computed(() => this.response()?.answer ?? '');
}
