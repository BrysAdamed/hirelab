import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  linkedSignal,
  signal,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideArrowDown,
  lucideArrowLeft,
  lucideArrowUp,
  lucideCircleAlert,
  lucidePlus,
  lucideTrash2,
  lucideX,
} from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import { HlmDialogImports } from '@hirelab/ui/helm/dialog';
import { HlmInput } from '@hirelab/ui/helm/input';
import { HlmLabel } from '@hirelab/ui/helm/label';
import type { ApiResult } from '../api/api-result';
import type { InterviewSession } from '../api/staff-api';
import { fromLocalInput, toLocalInput } from './schedule';
import { SessionStateBadge } from './session-state-badge';
import { SessionsService } from './sessions.service';
import { TaskPicker } from './task-picker';

/** A session: its schedule and, while it is planned, its editable task queue. */
@Component({
  selector: 'hl-session-page',
  imports: [
    DatePipe,
    RouterLink,
    NgIcon,
    HlmButton,
    HlmCardImports,
    HlmDialogImports,
    HlmInput,
    HlmLabel,
    SessionStateBadge,
    TaskPicker,
  ],
  providers: [
    provideIcons({
      lucideArrowDown,
      lucideArrowLeft,
      lucideArrowUp,
      lucideCircleAlert,
      lucidePlus,
      lucideTrash2,
      lucideX,
    }),
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (session(); as session) {
      <a
        hlmBtn
        variant="link"
        size="sm"
        class="px-0"
        [routerLink]="['/candidates', session.candidateId]"
      >
        <ng-icon name="lucideArrowLeft" aria-hidden="true" />
        {{ session.candidateDisplayName }}
      </a>
    }

    @if (error(); as message) {
      <div role="alert" class="mt-3 flex items-start gap-2 rounded-md border border-destructive p-2">
        <ng-icon
          name="lucideCircleAlert"
          size="16"
          class="mt-0.5 shrink-0 text-destructive"
          aria-hidden="true"
        />
        <p>{{ message }}</p>
      </div>
    }

    @if (session(); as session) {
      <div class="mt-2 flex flex-wrap items-start justify-between gap-2">
        <div class="flex flex-col gap-1">
          <div class="flex flex-wrap items-center gap-2">
            <h1 class="text-xl font-semibold">
              Session {{ session.number }} · {{ session.candidateDisplayName }}
            </h1>
            <hl-session-state-badge [state]="session.state" />
          </div>
          <p class="text-muted-foreground">
            Planned by {{ session.createdByName ?? 'a former user' }} on
            {{ session.createdAt | date: 'mediumDate' }}
          </p>
        </div>
        @if (planned()) {
          <hlm-dialog>
            <button hlmDialogTrigger hlmBtn type="button" variant="outline" size="sm" [disabled]="pending()">
              <ng-icon name="lucideTrash2" aria-hidden="true" />
              Delete
            </button>
            <ng-template hlmDialogPortal>
              <hlm-dialog-content>
                <hlm-dialog-header>
                  <h3 hlmDialogTitle>Delete Session {{ session.number }}</h3>
                  <p hlmDialogDescription>
                    The session and its queue are deleted for good. This can’t be undone.
                  </p>
                </hlm-dialog-header>
                <hlm-dialog-footer>
                  <button hlmBtn hlmDialogClose type="button" variant="outline">Cancel</button>
                  <button
                    hlmBtn
                    hlmDialogClose
                    type="button"
                    variant="destructive"
                    (click)="deleteSession(session)"
                  >
                    Delete session
                  </button>
                </hlm-dialog-footer>
              </hlm-dialog-content>
            </ng-template>
          </hlm-dialog>
        }
      </div>

      <section hlmCard class="mt-3" aria-labelledby="schedule-title">
        <div hlmCardHeader>
          <h2 hlmCardTitle id="schedule-title">Schedule</h2>
        </div>
        <div hlmCardContent>
          @if (planned()) {
            <form class="flex flex-wrap items-end gap-2" (submit)="$event.preventDefault(); saveSchedule()">
              <div class="flex flex-col gap-1">
                <label hlmLabel for="session-scheduled-at">Date and time</label>
                <input
                  hlmInput
                  id="session-scheduled-at"
                  type="datetime-local"
                  [value]="scheduleInput()"
                  (input)="scheduleInput.set($any($event.target).value)"
                />
              </div>
              <button hlmBtn type="submit" variant="secondary" [disabled]="pending()">Save schedule</button>
              @if (session.scheduledAt) {
                <button hlmBtn type="button" variant="ghost" [disabled]="pending()" (click)="clearSchedule()">
                  <ng-icon name="lucideX" aria-hidden="true" />
                  Clear
                </button>
              }
            </form>
          } @else if (session.scheduledAt; as scheduledAt) {
            <p>{{ scheduledAt | date: 'medium' }}</p>
          } @else {
            <p class="text-muted-foreground">Not scheduled</p>
          }
        </div>
      </section>

      <section hlmCard class="mt-3" aria-labelledby="queue-title">
        <div hlmCardHeader class="flex flex-row flex-wrap items-center justify-between gap-2">
          <h2 hlmCardTitle id="queue-title">Task queue</h2>
          @if (planned()) {
            <hlm-dialog>
              <button hlmDialogTrigger hlmBtn type="button" size="sm" variant="outline">
                <ng-icon name="lucidePlus" aria-hidden="true" />
                Add to queue
              </button>
              <ng-template hlmDialogPortal>
                <hlm-dialog-content class="max-h-[85dvh] overflow-y-auto sm:max-w-3xl">
                  <hlm-dialog-header>
                    <h3 hlmDialogTitle>Add tasks to Session {{ session.number }}</h3>
                    <p hlmDialogDescription>
                      Each task is queued with its active version and added to the end of the queue.
                    </p>
                  </hlm-dialog-header>
                  <hl-task-picker [queuedTaskIds]="queuedTaskIds()" (picked)="addTask(session, $event)" />
                  <hlm-dialog-footer>
                    <button hlmBtn hlmDialogClose type="button" variant="outline">Done</button>
                  </hlm-dialog-footer>
                </hlm-dialog-content>
              </ng-template>
            </hlm-dialog>
          }
        </div>
        <div hlmCardContent>
          @if (session.tasks.length === 0) {
            <p class="rounded-md border border-dashed p-3 text-center text-muted-foreground">
              The queue is empty. Add tasks from the task bank.
            </p>
          } @else {
            <ol class="flex flex-col divide-y rounded-md border" aria-label="Task queue">
              @for (task of session.tasks; track task.id; let index = $index, last = $last) {
                <li class="flex flex-wrap items-center justify-between gap-2 px-3 py-2">
                  <div class="flex min-w-0 flex-1 items-start gap-3">
                    <span class="text-muted-foreground">{{ index + 1 }}.</span>
                    <div class="flex min-w-0 flex-col">
                      <span class="flex flex-wrap items-center gap-2">
                        <a
                          class="rounded-sm font-medium text-accent-text underline-offset-4 hover:underline focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
                          [routerLink]="['/tasks', task.taskId]"
                        >
                          {{ task.title }}
                        </a>
                        <span class="font-mono text-muted-foreground">v{{ task.versionNumber }}</span>
                      </span>
                      <span class="line-clamp-1 text-muted-foreground" [title]="task.summary">
                        {{ task.summary }}
                      </span>
                    </div>
                  </div>
                  @if (planned()) {
                    <div class="flex gap-1">
                      <button
                        hlmBtn
                        type="button"
                        variant="ghost"
                        size="icon-sm"
                        [disabled]="pending() || index === 0"
                        [attr.aria-label]="'Move ' + task.title + ' up'"
                        (click)="move(session, index, -1)"
                      >
                        <ng-icon name="lucideArrowUp" aria-hidden="true" />
                      </button>
                      <button
                        hlmBtn
                        type="button"
                        variant="ghost"
                        size="icon-sm"
                        [disabled]="pending() || last"
                        [attr.aria-label]="'Move ' + task.title + ' down'"
                        (click)="move(session, index, 1)"
                      >
                        <ng-icon name="lucideArrowDown" aria-hidden="true" />
                      </button>
                      <button
                        hlmBtn
                        type="button"
                        variant="ghost"
                        size="icon-sm"
                        [disabled]="pending()"
                        [attr.aria-label]="'Remove ' + task.title + ' from the queue'"
                        (click)="removeTask(session, task.id)"
                      >
                        <ng-icon name="lucideTrash2" aria-hidden="true" />
                      </button>
                    </div>
                  }
                </li>
              }
            </ol>
          }
        </div>
      </section>
    }
  `,
})
export class SessionPage {
  private readonly sessionsService = inject(SessionsService);
  private readonly router = inject(Router);

  /** The `:sessionId` route parameter. */
  readonly sessionId = input.required<string>();

  protected readonly session = signal<InterviewSession | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly pending = signal(false);
  protected readonly planned = computed(() => this.session()?.state === 'Planned');
  protected readonly queuedTaskIds = computed(() => this.session()?.tasks.map((task) => task.taskId) ?? []);
  protected readonly scheduleInput = linkedSignal(() => toLocalInput(this.session()?.scheduledAt));

  constructor() {
    effect(() => void this.load(this.sessionId()));
  }

  protected saveSchedule(): Promise<void> {
    const session = this.session();
    return session
      ? this.change(() => this.sessionsService.reschedule(session.id, fromLocalInput(this.scheduleInput())))
      : Promise.resolve();
  }

  protected clearSchedule(): Promise<void> {
    const session = this.session();
    return session ? this.change(() => this.sessionsService.reschedule(session.id, null)) : Promise.resolve();
  }

  protected addTask(session: InterviewSession, taskId: string): Promise<void> {
    return this.change(() => this.sessionsService.addTask(session.id, taskId));
  }

  protected removeTask(session: InterviewSession, sessionTaskId: string): Promise<void> {
    return this.change(() => this.sessionsService.removeTask(session.id, sessionTaskId));
  }

  protected move(session: InterviewSession, index: number, offset: -1 | 1): Promise<void> {
    const order = session.tasks.map((task) => task.id);
    const target = index + offset;
    [order[index], order[target]] = [order[target], order[index]];
    return this.change(() => this.sessionsService.reorder(session.id, order));
  }

  protected async deleteSession(session: InterviewSession): Promise<void> {
    this.pending.set(true);
    const result = await this.sessionsService.delete(session.id);
    this.pending.set(false);
    if (result.ok) {
      await this.router.navigate(['/candidates', session.candidateId]);
    } else {
      this.error.set(result.message);
    }
  }

  protected async load(sessionId: string): Promise<void> {
    const result = await this.sessionsService.get(sessionId);
    if (result.ok) {
      this.error.set(null);
      this.session.set(result.value);
    } else {
      this.error.set(result.message);
      this.session.set(null);
    }
  }

  private async change(call: () => Promise<ApiResult<void>>): Promise<void> {
    this.pending.set(true);
    const result = await call();
    this.pending.set(false);
    await this.load(this.sessionId());
    if (!result.ok) {
      this.error.set(result.message);
    }
  }
}
