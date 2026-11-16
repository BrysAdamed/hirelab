import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert, lucideExternalLink } from '@ng-icons/lucide';
import type { TaskContent } from '@hirelab/tasks/framework';
import { ReviewTaskOutlet, type ReviewTaskOutletState } from '@hirelab/tasks/review';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import { MarkdownViewer } from '@hirelab/ui/markdown';
import type { SessionTaskDetail } from '../api/staff-api';
import type { ResponseUpdate } from './session-live-updates';
import { SessionTaskStateBadge } from './session-task-state-badge';
import { SessionsService } from './sessions.service';

/**
 * One session task as the interviewer sees it: the candidate's response, read-only, through its task type's `review`
 * component, with the reference answer and interviewer notes of the version it pins. A task type without a `review`
 * component shows the reference answer and notes alone.
 */
@Component({
  selector: 'hl-session-task-pane',
  imports: [
    RouterLink,
    NgIcon,
    HlmButton,
    HlmCardImports,
    MarkdownViewer,
    ReviewTaskOutlet,
    SessionTaskStateBadge,
  ],
  providers: [provideIcons({ lucideCircleAlert, lucideExternalLink })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section hlmCard aria-labelledby="session-task-title">
      @if (task(); as task) {
        <div hlmCardHeader class="flex flex-row flex-wrap items-center justify-between gap-2">
          <div class="flex min-w-0 flex-wrap items-center gap-2">
            <h2 hlmCardTitle id="session-task-title">{{ task.title }}</h2>
            <span class="font-mono text-muted-foreground">v{{ task.versionNumber }}</span>
            <hl-session-task-state-badge
              [state]="task.state"
              [sessionFinished]="sessionFinished()"
            />
          </div>
          <div class="flex items-center gap-2">
            <a hlmBtn variant="link" size="sm" class="px-0" [routerLink]="['/tasks', task.taskId]">
              <ng-icon name="lucideExternalLink" aria-hidden="true" />
              Task bank
            </a>
          </div>
        </div>
        <div hlmCardContent class="flex flex-col gap-4">
          <p class="whitespace-pre-line text-muted-foreground">{{ task.summary }}</p>
          @switch (reviewState()) {
            @case ('loading') {
              <p role="status" class="text-muted-foreground">Loading the response…</p>
            }
            @case ('failed') {
              <p
                role="alert"
                class="flex items-center gap-2 rounded-md border border-destructive p-2"
              >
                <ng-icon
                  name="lucideCircleAlert"
                  size="16"
                  class="shrink-0 text-destructive"
                  aria-hidden="true"
                />
                The response view could not be loaded. Reload the page to try again.
              </p>
            }
          }
          @if (reviewState() === 'unavailable' || reviewState() === 'failed') {
            <p class="text-muted-foreground">This task type has no response view here.</p>
            <section aria-labelledby="session-task-reference-answer">
              <h3 id="session-task-reference-answer" class="font-semibold">Reference answer</h3>
              <hl-markdown class="mt-1" [markdown]="task.referenceAnswer" />
            </section>
            @if (task.interviewerNotes) {
              <section aria-labelledby="session-task-interviewer-notes">
                <h3 id="session-task-interviewer-notes" class="font-semibold">Interviewer notes</h3>
                <hl-markdown class="mt-1" [markdown]="task.interviewerNotes" />
              </section>
            }
          }
          <div [class.hidden]="reviewState() !== 'ready'">
            <hl-review-task-outlet
              [taskType]="task.taskType"
              [content]="content()!"
              [response]="task.response ?? null"
              [referenceAnswer]="task.referenceAnswer"
              [interviewerNotes]="task.interviewerNotes ?? undefined"
              [live]="live()"
              (stateChange)="reviewState.set($event)"
            />
          </div>
        </div>
      } @else if (error(); as message) {
        <div hlmCardContent>
          <div role="alert" class="flex items-start gap-2 rounded-md border border-destructive p-2">
            <ng-icon
              name="lucideCircleAlert"
              size="16"
              class="mt-0.5 shrink-0 text-destructive"
              aria-hidden="true"
            />
            <p>{{ message }}</p>
          </div>
        </div>
      } @else {
        <div hlmCardContent>
          <h2 id="session-task-title" class="sr-only">Selected task</h2>
          <p class="text-muted-foreground">Loading the task…</p>
        </div>
      }
    </section>
  `,
})
export class SessionTaskPane {
  private readonly sessionsService = inject(SessionsService);

  readonly sessionId = input.required<string>();
  readonly sessionTaskId = input.required<string>();
  /** The session's queue revision: a new one refetches the task, whose state or pin may have changed. */
  readonly queueRevision = input.required<number>();
  readonly sessionLive = input(false);
  readonly sessionFinished = input(false);
  /** The latest response revision the staff hub announced: a newer one for this task refetches it. */
  readonly responseUpdate = input<ResponseUpdate | null>(null);
  /** Raised after the live connection resubscribed, when anything may have changed unseen: refetches the task. */
  readonly resync = input(0);

  protected readonly task = signal<SessionTaskDetail | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly reviewState = signal<ReviewTaskOutletState>('loading');
  protected readonly content = computed<TaskContent<unknown, unknown> | null>(() => {
    const task = this.task();
    return task ? { candidate: task.candidateContent, staff: task.staffContent } : null;
  });
  /** Whether the candidate may still be changing the response. */
  protected readonly live = computed(() => this.sessionLive() && this.task()?.state === 'Current');

  constructor() {
    effect(() => {
      const sessionId = this.sessionId();
      const sessionTaskId = this.sessionTaskId();
      this.queueRevision();
      this.resync();
      untracked(() => void this.load(sessionId, sessionTaskId));
    });
    effect(() => {
      const update = this.responseUpdate();
      untracked(() => {
        const task = this.task();
        if (
          update &&
          task &&
          update.sessionTaskId === task.id &&
          update.revision > task.responseRevision
        ) {
          void this.load(this.sessionId(), task.id);
        }
      });
    });
  }

  private async load(sessionId: string, sessionTaskId: string): Promise<void> {
    if (this.task()?.id !== sessionTaskId) {
      this.task.set(null);
    }

    const result = await this.sessionsService.getTask(sessionId, sessionTaskId);
    if (sessionTaskId !== this.sessionTaskId()) {
      return;
    }

    if (result.ok) {
      this.error.set(null);
      this.task.set(result.value);
    } else {
      this.task.set(null);
      this.error.set(result.message);
    }
  }
}
