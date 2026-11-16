import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert, lucideUserPlus } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import type { JobPosition, PositionCandidate } from '../api/staff-api';
import { CandidatesService } from './candidates.service';

/** A job position's candidates card: the candidates table and, while the position is open, "Add candidate". */
@Component({
  selector: 'hl-position-candidates',
  imports: [DatePipe, RouterLink, NgIcon, HlmButton, HlmCardImports],
  providers: [provideIcons({ lucideCircleAlert, lucideUserPlus })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section hlmCard aria-labelledby="candidates-title">
      <div hlmCardHeader class="flex flex-row flex-wrap items-center justify-between gap-2">
        <h2 hlmCardTitle id="candidates-title">Candidates</h2>
        @if (position().state === 'Open') {
          <a
            hlmBtn
            size="sm"
            variant="outline"
            [routerLink]="['/positions', position().id, 'candidates', 'new']"
          >
            <ng-icon name="lucideUserPlus" aria-hidden="true" />
            Add candidate
          </a>
        }
      </div>
      <div hlmCardContent>
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
        } @else if (loaded()) {
          @if (candidates().length === 0) {
            <p class="rounded-md border border-dashed p-3 text-center text-muted-foreground">
              No candidates yet.
            </p>
          } @else {
            <div class="overflow-x-auto rounded-md border">
              <table class="w-full text-left">
                <caption class="sr-only">
                  Candidates for {{ position().title }}
                </caption>
                <thead class="border-b bg-muted text-muted-foreground">
                  <tr>
                    <th scope="col" class="px-3 py-2 font-medium">#</th>
                    <th scope="col" class="px-3 py-2 font-medium">Name</th>
                    <th scope="col" class="px-3 py-2 font-medium">Email</th>
                    <th scope="col" class="px-3 py-2 font-medium">Phone</th>
                    <th scope="col" class="px-3 py-2 font-medium">Added</th>
                  </tr>
                </thead>
                <tbody>
                  @for (candidate of candidates(); track candidate.id) {
                    <tr class="border-b last:border-b-0">
                      <td class="px-3 py-2 text-muted-foreground">{{ candidate.number }}</td>
                      <td class="px-3 py-2">
                        <a
                          class="rounded-sm font-medium text-accent-text underline-offset-4 hover:underline focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
                          [routerLink]="['/candidates', candidate.id]"
                        >
                          {{ candidate.displayName }}
                        </a>
                      </td>
                      <td class="px-3 py-2">{{ candidate.email ?? '—' }}</td>
                      <td class="px-3 py-2">{{ candidate.phone ?? '—' }}</td>
                      <td class="px-3 py-2">{{ candidate.createdAt | date: 'mediumDate' }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        }
      </div>
    </section>
  `,
})
export class PositionCandidates {
  private readonly candidatesService = inject(CandidatesService);
  private latestRequest = 0;

  readonly position = input.required<JobPosition>();

  protected readonly candidates = signal<PositionCandidate[]>([]);
  protected readonly loaded = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    effect(() => void this.load(this.position().id));
  }

  private async load(positionId: string): Promise<void> {
    const request = ++this.latestRequest;
    const result = await this.candidatesService.list(positionId);
    if (request !== this.latestRequest) {
      return;
    }
    this.loaded.set(true);
    if (result.ok) {
      this.error.set(null);
      this.candidates.set(result.value);
    } else {
      this.error.set(result.message);
      this.candidates.set([]);
    }
  }
}
