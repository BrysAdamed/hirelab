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
import {
  lucideArrowLeft,
  lucideCircleAlert,
  lucideExternalLink,
  lucideLock,
  lucidePencil,
  lucideUserX,
} from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import type { Candidate } from '../api/staff-api';
import { CandidatesService } from './candidates.service';

/** A candidate's profile: their position and contact details. */
@Component({
  selector: 'hl-candidate-profile-page',
  imports: [DatePipe, RouterLink, NgIcon, HlmButton, HlmCardImports],
  providers: [
    provideIcons({
      lucideArrowLeft,
      lucideCircleAlert,
      lucideExternalLink,
      lucideLock,
      lucidePencil,
      lucideUserX,
    }),
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (candidate(); as candidate) {
      <a
        hlmBtn
        variant="link"
        size="sm"
        class="px-0"
        [routerLink]="['/positions', candidate.positionId]"
      >
        <ng-icon name="lucideArrowLeft" aria-hidden="true" />
        {{ candidate.positionTitle }}
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

    @if (candidate(); as candidate) {
      <div class="mt-2 flex flex-wrap items-start justify-between gap-2">
        <div class="flex flex-col gap-1">
          <h1 class="text-xl font-semibold">{{ candidate.displayName }}</h1>
          <p class="text-muted-foreground">
            Candidate #{{ candidate.number }} · Added {{ candidate.createdAt | date: 'mediumDate' }}
          </p>
        </div>
        @if (canEdit()) {
          <a hlmBtn variant="outline" size="sm" [routerLink]="['/candidates', candidate.id, 'edit']">
            <ng-icon name="lucidePencil" aria-hidden="true" />
            Edit
          </a>
        }
      </div>

      @if (candidate.isAnonymised) {
        <p class="mt-3 flex items-center gap-2 rounded-md border p-2 text-muted-foreground">
          <ng-icon name="lucideUserX" size="16" aria-hidden="true" />
          This candidate has been anonymised. Their contact details were removed.
        </p>
      } @else if (candidate.positionState === 'Closed') {
        <p class="mt-3 flex items-center gap-2 rounded-md border p-2 text-muted-foreground">
          <ng-icon name="lucideLock" size="16" aria-hidden="true" />
          The position is closed, so this candidate is read-only.
        </p>
      }

      <section hlmCard class="mt-3" aria-labelledby="contact-details-title">
        <div hlmCardHeader>
          <h2 hlmCardTitle id="contact-details-title">Contact details</h2>
        </div>
        <div hlmCardContent>
          <dl class="grid gap-x-4 gap-y-2 sm:grid-cols-[10rem_1fr]">
            <dt class="text-muted-foreground">Email</dt>
            <dd>
              @if (candidate.email; as email) {
                <a class="text-accent-text underline-offset-4 hover:underline" [href]="'mailto:' + email">
                  {{ email }}
                </a>
              } @else {
                <span class="text-muted-foreground">Not given</span>
              }
            </dd>
            <dt class="text-muted-foreground">Phone</dt>
            <dd>
              @if (candidate.phone; as phone) {
                <a class="text-accent-text underline-offset-4 hover:underline" [href]="'tel:' + phone">
                  {{ phone }}
                </a>
              } @else {
                <span class="text-muted-foreground">Not given</span>
              }
            </dd>
            <dt class="text-muted-foreground">Profile links</dt>
            <dd>
              @if (candidate.links.length > 0) {
                <ul class="flex flex-col gap-1">
                  @for (link of candidate.links; track $index) {
                    <li>
                      <a
                        class="inline-flex items-center gap-1 break-all text-accent-text underline-offset-4 hover:underline"
                        target="_blank"
                        rel="noopener noreferrer"
                        [href]="link"
                      >
                        {{ link }}
                        <ng-icon name="lucideExternalLink" size="14" aria-hidden="true" />
                        <span class="sr-only">(opens in a new tab)</span>
                      </a>
                    </li>
                  }
                </ul>
              } @else {
                <span class="text-muted-foreground">None</span>
              }
            </dd>
          </dl>
        </div>
      </section>
    }
  `,
})
export class CandidateProfilePage {
  private readonly candidatesService = inject(CandidatesService);

  /** The `:candidateId` route parameter. */
  readonly candidateId = input.required<string>();

  protected readonly candidate = signal<Candidate | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly canEdit = computed(() => {
    const candidate = this.candidate();
    return candidate?.positionState === 'Open' && !candidate.isAnonymised;
  });

  constructor() {
    effect(() => void this.load(this.candidateId()));
  }

  private async load(candidateId: string): Promise<void> {
    const result = await this.candidatesService.get(candidateId);
    if (result.ok) {
      this.error.set(null);
      this.candidate.set(result.value);
    } else {
      this.error.set(result.message);
      this.candidate.set(null);
    }
  }
}
