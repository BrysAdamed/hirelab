import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert, lucidePlus } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import type { JobPositionState, JobPositionSummary } from '../api/staff-api';
import { AuthService } from '../auth/auth.service';
import { PositionStateBadge } from './position-state-badge';
import { PositionsService } from './positions.service';

const filters: readonly { state: JobPositionState; label: string; query: string | null }[] = [
  { state: 'Open', label: 'Open', query: null },
  { state: 'Closed', label: 'Closed', query: 'closed' },
];

/** The team's job positions, open ones by default; `?state=closed` shows the closed ones. */
@Component({
  selector: 'hl-positions-page',
  imports: [DatePipe, RouterLink, NgIcon, HlmButton, PositionStateBadge],
  providers: [provideIcons({ lucideCircleAlert, lucidePlus })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-wrap items-center justify-between gap-2">
      <h1 class="text-xl font-semibold">Job positions</h1>
      @if (isManager()) {
        <a hlmBtn size="sm" routerLink="/positions/new">
          <ng-icon name="lucidePlus" aria-hidden="true" />
          New position
        </a>
      }
    </div>

    <div role="group" aria-label="Filter by state" class="mt-3 inline-flex gap-1 rounded-md border p-1">
      @for (filter of filters; track filter.state) {
        <a
          hlmBtn
          size="sm"
          routerLink="/positions"
          [queryParams]="{ state: filter.query }"
          [variant]="filter.state === selected() ? 'secondary' : 'ghost'"
          [attr.aria-current]="filter.state === selected() ? 'page' : null"
        >
          {{ filter.label }}
        </a>
      }
    </div>

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

    @if (loaded()) {
      @if (positions().length === 0) {
        <p class="mt-3 rounded-md border border-dashed p-3 text-center text-muted-foreground">
          {{ selected() === 'Open' ? 'No open positions.' : 'No closed positions.' }}
        </p>
      } @else {
        <div class="mt-3 overflow-x-auto rounded-md border">
          <table class="w-full text-left">
            <caption class="sr-only">
              {{
                selected()
              }}
              job positions
            </caption>
            <thead class="border-b bg-muted text-muted-foreground">
              <tr>
                <th scope="col" class="px-3 py-2 font-medium">Title</th>
                <th scope="col" class="px-3 py-2 font-medium">State</th>
                <th scope="col" class="px-3 py-2 font-medium">Created</th>
              </tr>
            </thead>
            <tbody>
              @for (position of positions(); track position.id) {
                <tr class="border-b last:border-b-0">
                  <td class="px-3 py-2">
                    <a
                      class="rounded-sm font-medium text-accent-text underline-offset-4 hover:underline focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
                      [routerLink]="['/positions', position.id]"
                    >
                      {{ position.title }}
                    </a>
                  </td>
                  <td class="px-3 py-2"><hl-position-state-badge [state]="position.state" /></td>
                  <td class="px-3 py-2">{{ position.createdAt | date: 'mediumDate' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    }
  `,
})
export class PositionsPage {
  private readonly positionsService = inject(PositionsService);
  private readonly auth = inject(AuthService);
  private latestRequest = 0;

  /** The `state` query parameter: `closed`, or nothing for open positions. */
  readonly state = input<string | undefined>();

  protected readonly filters = filters;
  protected readonly selected = computed<JobPositionState>(() =>
    this.state()?.toLowerCase() === 'closed' ? 'Closed' : 'Open',
  );
  protected readonly isManager = computed(() => this.auth.user()?.role === 'Manager');
  protected readonly positions = signal<JobPositionSummary[]>([]);
  protected readonly loaded = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    effect(() => void this.load(this.selected()));
  }

  private async load(state: JobPositionState): Promise<void> {
    const request = ++this.latestRequest;
    const result = await this.positionsService.list(state);
    if (request !== this.latestRequest) {
      return;
    }
    this.loaded.set(true);
    if (result.ok) {
      this.error.set(null);
      this.positions.set(result.value);
    } else {
      this.error.set(result.message);
      this.positions.set([]);
    }
  }
}
