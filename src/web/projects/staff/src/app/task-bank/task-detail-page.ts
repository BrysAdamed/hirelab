import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft, lucideCircleAlert } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import { MarkdownViewer } from '@hirelab/ui/markdown';
import type { BankTask } from '../api/staff-api';
import { TaskBankService } from './task-bank.service';
import { TaskFacts } from './task-facts';
import { TaskPreview } from './task-preview';
import { TaskRubric } from './task-rubric';
import { TaskVisibilityBadge } from './task-visibility-badge';

/** A task in the task bank: its active version's metadata, staff material, rubric and a candidate preview. */
@Component({
  selector: 'hl-task-detail-page',
  imports: [
    RouterLink,
    NgIcon,
    HlmButton,
    HlmCardImports,
    MarkdownViewer,
    TaskFacts,
    TaskPreview,
    TaskRubric,
    TaskVisibilityBadge,
  ],
  providers: [provideIcons({ lucideArrowLeft, lucideCircleAlert })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a hlmBtn variant="link" size="sm" class="px-0" routerLink="/tasks">
      <ng-icon name="lucideArrowLeft" aria-hidden="true" />
      Task bank
    </a>

    @if (error(); as message) {
      <div
        role="alert"
        class="mt-3 flex items-start gap-2 rounded-md border border-destructive p-2"
      >
        <ng-icon
          name="lucideCircleAlert"
          size="16"
          class="mt-0.5 shrink-0 text-destructive"
          aria-hidden="true"
        />
        <p>{{ message }}</p>
      </div>
    }

    @if (task(); as task) {
      <div class="mt-2 flex flex-col gap-2">
        <div class="flex flex-wrap items-center gap-2">
          <h1 class="text-xl font-semibold">{{ task.title }}</h1>
          <span class="font-mono text-muted-foreground">v{{ task.versionNumber }}</span>
          <hl-task-visibility-badge [visibility]="task.visibility" />
        </div>
        <hl-task-facts [task]="task" />
        <p class="whitespace-pre-line">{{ task.summary }}</p>
      </div>

      <section hlmCard class="mt-3" aria-label="Staff material">
        <div hlmCardContent class="flex flex-col gap-4">
          <section aria-labelledby="task-reference-answer">
            <h2 id="task-reference-answer" class="font-semibold">Reference answer</h2>
            <hl-markdown class="mt-1" [markdown]="task.referenceAnswer" />
          </section>
          @if (task.interviewerNotes) {
            <section aria-labelledby="task-interviewer-notes">
              <h2 id="task-interviewer-notes" class="font-semibold">Interviewer notes</h2>
              <hl-markdown class="mt-1" [markdown]="task.interviewerNotes" />
            </section>
          }
        </div>
      </section>

      <section hlmCard class="mt-3" aria-labelledby="task-rubric">
        <div hlmCardContent>
          <h2 id="task-rubric" class="font-semibold">Rubric</h2>
          <hl-task-rubric class="mt-2 block" [rubric]="task.rubric" />
        </div>
      </section>

      <section hlmCard class="mt-3" aria-labelledby="task-preview">
        <div hlmCardContent>
          <h2 id="task-preview" class="font-semibold">Candidate preview</h2>
          <hl-task-preview
            class="mt-1 block"
            [taskType]="task.taskType"
            [content]="task.candidateContent"
          />
        </div>
      </section>
    }
  `,
})
export class TaskDetailPage {
  private readonly taskBank = inject(TaskBankService);

  /** The `:taskId` route parameter. */
  readonly taskId = input.required<string>();

  protected readonly task = signal<BankTask | null>(null);
  protected readonly error = signal<string | null>(null);

  constructor() {
    effect(() => void this.load(this.taskId()));
  }

  private async load(taskId: string): Promise<void> {
    const result = await this.taskBank.get(taskId);
    if (result.ok) {
      this.error.set(null);
      this.task.set(result.value);
    } else {
      this.error.set(result.message);
      this.task.set(null);
    }
  }
}
