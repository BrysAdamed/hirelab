import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert } from '@ng-icons/lucide';
import type { TeamSession } from '../api/staff-api';
import { SessionStateBadge } from './session-state-badge';
import { SessionsService } from './sessions.service';

/** The team's Planned and Live sessions, soonest first. */
@Component({
  selector: 'hl-sessions-page',
  imports: [DatePipe, RouterLink, NgIcon, SessionStateBadge],
  providers: [provideIcons({ lucideCircleAlert })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="text-xl font-semibold">Sessions</h1>
    <p class="mt-1 text-muted-foreground">
      Planned and live sessions, soonest first. Plan a new one from a candidate's profile.
    </p>

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
    } @else if (loaded()) {
      @if (sessions().length === 0) {
        <p class="mt-3 rounded-md border border-dashed p-3 text-center text-muted-foreground">
          No planned or live sessions.
        </p>
      } @else {
        <div class="mt-3 overflow-x-auto rounded-md border">
          <table class="w-full text-left">
            <caption class="sr-only">
              Planned and live sessions
            </caption>
            <thead class="border-b bg-muted text-muted-foreground">
              <tr>
                <th scope="col" class="px-3 py-2 font-medium">Candidate</th>
                <th scope="col" class="px-3 py-2 font-medium">Session</th>
                <th scope="col" class="px-3 py-2 font-medium">Scheduled</th>
                <th scope="col" class="px-3 py-2 font-medium">Tasks</th>
                <th scope="col" class="px-3 py-2 font-medium">State</th>
              </tr>
            </thead>
            <tbody>
              @for (session of sessions(); track session.id) {
                <tr class="border-b last:border-b-0">
                  <td class="px-3 py-2">
                    <a
                      class="rounded-sm text-accent-text underline-offset-4 hover:underline focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
                      [routerLink]="['/candidates', session.candidateId]"
                    >
                      {{ session.candidateDisplayName }}
                    </a>
                  </td>
                  <td class="px-3 py-2">
                    <a
                      class="rounded-sm font-medium text-accent-text underline-offset-4 hover:underline focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
                      [routerLink]="['/sessions', session.id]"
                    >
                      Session {{ session.number }}
                    </a>
                  </td>
                  <td class="px-3 py-2">
                    @if (session.scheduledAt; as scheduledAt) {
                      {{ scheduledAt | date: 'medium' }}
                    } @else {
                      <span class="text-muted-foreground">Not scheduled</span>
                    }
                  </td>
                  <td class="px-3 py-2">{{ session.taskCount }}</td>
                  <td class="px-3 py-2"><hl-session-state-badge [state]="session.state" /></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    }
  `,
})
export class SessionsPage {
  private readonly sessionsService = inject(SessionsService);

  protected readonly sessions = signal<TeamSession[]>([]);
  protected readonly loaded = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    const result = await this.sessionsService.listTeam();
    this.loaded.set(true);
    if (result.ok) {
      this.sessions.set(result.value);
    } else {
      this.error.set(result.message);
    }
  }
}
