import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCalendarPlus, lucideCircleAlert } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import type { CandidateSession } from '../api/staff-api';
import { SessionStateBadge } from '../sessions/session-state-badge';
import { SessionsService } from '../sessions/sessions.service';

/** A candidate's interview sessions card: the sessions by number and, when allowed, "Plan session". */
@Component({
  selector: 'hl-candidate-sessions',
  imports: [DatePipe, RouterLink, NgIcon, HlmButton, HlmCardImports, SessionStateBadge],
  providers: [provideIcons({ lucideCalendarPlus, lucideCircleAlert })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section hlmCard aria-labelledby="sessions-title">
      <div hlmCardHeader class="flex flex-row flex-wrap items-center justify-between gap-2">
        <h2 hlmCardTitle id="sessions-title">Interview sessions</h2>
        @if (canPlan()) {
          <button
            hlmBtn
            type="button"
            size="sm"
            variant="outline"
            [disabled]="pending()"
            (click)="plan()"
          >
            <ng-icon name="lucideCalendarPlus" aria-hidden="true" />
            Plan session
          </button>
        }
      </div>
      <div hlmCardContent class="flex flex-col gap-2">
        @if (error(); as message) {
          <div role="alert" class="flex items-start gap-2 rounded-md border border-destructive p-2">
            <ng-icon
              name="lucideCircleAlert"
              size="16"
              class="mt-0.5 shrink-0 text-destructive"
              aria-hidden="true"
            />
            <p>{{ message }}</p>
          </div>
        }
        @if (loaded()) {
          @if (sessions().length === 0) {
            <p class="rounded-md border border-dashed p-3 text-center text-muted-foreground">
              No sessions yet.
            </p>
          } @else {
            <ul class="flex flex-col divide-y rounded-md border" aria-label="Sessions">
              @for (session of sessions(); track session.id) {
                <li class="flex flex-wrap items-center justify-between gap-2 px-3 py-2">
                  <div class="flex flex-col">
                    <a
                      class="rounded-sm font-medium text-accent-text underline-offset-4 hover:underline focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
                      [routerLink]="['/sessions', session.id]"
                    >
                      Session {{ session.number }}
                    </a>
                    <span class="text-muted-foreground">
                      @if (session.scheduledAt; as scheduledAt) {
                        {{ scheduledAt | date: 'medium' }}
                      } @else {
                        Not scheduled
                      }
                      · {{ session.taskCount === 1 ? '1 task' : session.taskCount + ' tasks' }}
                    </span>
                  </div>
                  <hl-session-state-badge [state]="session.state" />
                </li>
              }
            </ul>
          }
        }
      </div>
    </section>
  `,
})
export class CandidateSessions {
  private readonly sessionsService = inject(SessionsService);
  private readonly router = inject(Router);
  private latestRequest = 0;

  readonly candidateId = input.required<string>();
  /** Whether a new session may be planned: the candidate isn't anonymised and their position is open. */
  readonly canPlan = input(false);

  protected readonly sessions = signal<CandidateSession[]>([]);
  protected readonly loaded = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly pending = signal(false);

  constructor() {
    effect(() => void this.load(this.candidateId()));
  }

  protected async plan(): Promise<void> {
    this.pending.set(true);
    const result = await this.sessionsService.create(this.candidateId(), null);
    this.pending.set(false);
    if (result.ok) {
      await this.router.navigate(['/sessions', result.value.id]);
    } else {
      this.error.set(result.message);
    }
  }

  private async load(candidateId: string): Promise<void> {
    const request = ++this.latestRequest;
    const result = await this.sessionsService.listForCandidate(candidateId);
    if (request !== this.latestRequest) {
      return;
    }
    this.loaded.set(true);
    if (result.ok) {
      this.error.set(null);
      this.sessions.set(result.value);
    } else {
      this.error.set(result.message);
      this.sessions.set([]);
    }
  }
}
