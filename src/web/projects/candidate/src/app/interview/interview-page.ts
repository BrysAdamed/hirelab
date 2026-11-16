import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideCircleAlert,
  lucideCircleCheck,
  lucideClock,
  lucideLink2Off,
  lucideLoaderCircle,
  lucideRotateCw,
} from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { CandidateTaskOutlet, type CandidateTaskOutletState } from '@hirelab/tasks/candidate';
import type { CurrentTask } from '../api/candidate-api';
import { InterviewService, type InterviewResult } from './interview.service';
import { InterviewUpdates } from './interview-updates';

/**
 * The candidate's interview (ADR 0008): a waiting page until the session goes Live, then only the current task,
 * rendered by its task type's `candidate` component, and a closing screen after the last one. When the session has
 * finished, or the link no longer works, it says so. It follows the session through {@link InterviewUpdates}.
 */
@Component({
  selector: 'hl-interview-page',
  imports: [NgIcon, HlmButton, CandidateTaskOutlet],
  providers: [
    provideIcons({
      lucideCircleAlert,
      lucideCircleCheck,
      lucideClock,
      lucideLink2Off,
      lucideLoaderCircle,
      lucideRotateCw,
    }),
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'mx-auto flex w-full max-w-4xl flex-col gap-4 p-6' },
  template: `
    @let current = result();
    @if (!current) {
      <p role="status" class="flex items-center gap-2 text-muted-foreground">
        <ng-icon
          name="lucideLoaderCircle"
          size="20"
          class="motion-safe:animate-spin"
          aria-hidden="true"
        />
        Opening your interview…
      </p>
    } @else if (current.ok) {
      @switch (current.interview.stage) {
        @case ('Waiting') {
          <section class="flex flex-col gap-2" aria-labelledby="interview-title">
            <h1 id="interview-title" class="flex items-center gap-2 text-2xl font-semibold">
              <ng-icon name="lucideClock" size="20" aria-hidden="true" />
              Your interview hasn’t started yet
            </h1>
            <p>
              Keep this page open. The first task appears here when your interviewer starts the
              interview.
            </p>
          </section>
        }
        @case ('CurrentTask') {
          @if (task(); as task) {
            <section class="flex flex-col gap-4" aria-labelledby="interview-title">
              <h1 id="interview-title" class="sr-only">Current task</h1>
              @switch (taskState()) {
                @case ('loading') {
                  <p role="status" class="text-muted-foreground">Loading the task…</p>
                }
                @case ('unavailable') {
                  <p role="alert" class="flex items-center gap-2 rounded-md border p-4">
                    <ng-icon name="lucideCircleAlert" size="20" aria-hidden="true" />
                    This task can’t be shown here. Please tell your interviewer.
                  </p>
                }
                @case ('failed') {
                  <p
                    role="alert"
                    class="flex items-center gap-2 rounded-md border border-destructive p-4"
                  >
                    <ng-icon
                      name="lucideCircleAlert"
                      size="20"
                      class="text-destructive"
                      aria-hidden="true"
                    />
                    The task could not be loaded. Reload the page to try again.
                  </p>
                }
              }
              <div [class.hidden]="taskState() !== 'ready'">
                <hl-candidate-task-outlet
                  [taskType]="task.taskType"
                  [content]="task.content"
                  [(response)]="response"
                  (stateChange)="taskState.set($event)"
                />
              </div>
            </section>
          }
        }
        @case ('Closing') {
          <section class="flex flex-col gap-2" aria-labelledby="interview-title">
            <h1 id="interview-title" class="flex items-center gap-2 text-2xl font-semibold">
              <ng-icon name="lucideCircleCheck" size="20" aria-hidden="true" />
              That was the last task
            </h1>
            <p>Thank you. Your interviewer will wrap up the interview with you.</p>
          </section>
        }
      }
    } @else {
      <section class="flex flex-col gap-2" aria-labelledby="interview-title" role="alert">
        @switch (current.reason) {
          @case ('ended') {
            <h1 id="interview-title" class="flex items-center gap-2 text-2xl font-semibold">
              <ng-icon name="lucideCircleCheck" size="20" aria-hidden="true" />
              This interview has ended
            </h1>
            <p>Thank you for taking part. You can close this page.</p>
          }
          @case ('invalid-link') {
            <h1 id="interview-title" class="flex items-center gap-2 text-2xl font-semibold">
              <ng-icon name="lucideLink2Off" size="20" aria-hidden="true" />
              This link doesn’t work
            </h1>
            <p>
              It may have been replaced by a new one. Ask your interviewer for the current link.
            </p>
          }
          @case ('no-access') {
            <h1 id="interview-title" class="flex items-center gap-2 text-2xl font-semibold">
              <ng-icon name="lucideLink2Off" size="20" aria-hidden="true" />
              Open the link from your interviewer
            </h1>
            <p>This page works only through the interview link your interviewer sent you.</p>
          }
          @case ('rate-limited') {
            <h1 id="interview-title" class="flex items-center gap-2 text-2xl font-semibold">
              <ng-icon name="lucideCircleAlert" size="20" aria-hidden="true" />
              Too many attempts
            </h1>
            <p>Wait a minute, then open the link from your interviewer again.</p>
          }
          @case ('failed') {
            <h1 id="interview-title" class="flex items-center gap-2 text-2xl font-semibold">
              <ng-icon
                name="lucideCircleAlert"
                size="20"
                class="text-destructive"
                aria-hidden="true"
              />
              Something went wrong
            </h1>
            <p>Your interview could not be loaded. Check your connection and try again.</p>
            <div>
              <button hlmBtn type="button" variant="outline" (click)="retry()">
                <ng-icon name="lucideRotateCw" size="20" aria-hidden="true" />
                Try again
              </button>
            </div>
          }
        }
      </section>
    }
  `,
})
export class InterviewPage {
  private readonly interviews = inject(InterviewService);
  private readonly updates = inject(InterviewUpdates);
  private stopWatching: (() => void) | null = null;

  protected readonly result = signal<InterviewResult | null>(null);
  protected readonly task = computed<CurrentTask | null>(
    () => {
      const result = this.result();
      return result?.ok ? (result.interview.currentTask ?? null) : null;
    },
    { equal: (previous, next) => previous?.id === next?.id },
  );
  protected readonly taskState = signal<CandidateTaskOutletState>('loading');
  protected readonly response = linkedSignal<CurrentTask | null, unknown>({
    source: this.task,
    computation: () => null,
  });

  constructor() {
    inject(DestroyRef).onDestroy(() => this.stopWatching?.());
    void this.open();
  }

  protected retry(): void {
    this.result.set(null);
    void this.refresh();
  }

  private async open(): Promise<void> {
    this.show(await this.interviews.open());
  }

  private async refresh(): Promise<void> {
    this.show(await this.interviews.refresh());
  }

  private show(result: InterviewResult): void {
    this.result.set(result);
    const following = result.ok || result.reason === 'failed';
    if (following && !this.stopWatching) {
      this.stopWatching = this.updates.watch(() => void this.refresh());
    } else if (!following) {
      this.stopWatching?.();
      this.stopWatching = null;
    }
  }
}
