import { ChangeDetectionStrategy, Component, effect, inject, input, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert, lucideExternalLink } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import { MarkdownViewer } from '@hirelab/ui/markdown';
import type { SessionTaskDetail } from '../api/staff-api';
import { SessionTaskStateBadge } from './session-task-state-badge';
import { SessionsService } from './sessions.service';

/** The staff material of one session task, from the version it pins: reference answer and interviewer notes. */
@Component({
  selector: 'hl-session-task-pane',
  imports: [RouterLink, NgIcon, HlmButton, HlmCardImports, MarkdownViewer, SessionTaskStateBadge],
  providers: [provideIcons({ lucideCircleAlert, lucideExternalLink })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section hlmCard aria-labelledby="session-task-title">
      @if (task(); as task) {
        <div hlmCardHeader class="flex flex-row flex-wrap items-center justify-between gap-2">
          <div class="flex min-w-0 flex-wrap items-center gap-2">
            <h2 hlmCardTitle id="session-task-title">{{ task.title }}</h2>
            <span class="font-mono text-muted-foreground">v{{ task.versionNumber }}</span>
            <hl-session-task-state-badge [state]="task.state" [sessionFinished]="sessionFinished()" />
          </div>
          <a hlmBtn variant="link" size="sm" class="px-0" [routerLink]="['/tasks', task.taskId]">
            <ng-icon name="lucideExternalLink" aria-hidden="true" />
            Task bank
          </a>
        </div>
        <div hlmCardContent class="flex flex-col gap-4">
          <p class="whitespace-pre-line text-muted-foreground">{{ task.summary }}</p>
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
  readonly sessionFinished = input(false);

  protected readonly task = signal<SessionTaskDetail | null>(null);
  protected readonly error = signal<string | null>(null);

  constructor() {
    effect(() => {
      const sessionId = this.sessionId();
      const sessionTaskId = this.sessionTaskId();
      this.queueRevision();
      untracked(() => void this.load(sessionId, sessionTaskId));
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
