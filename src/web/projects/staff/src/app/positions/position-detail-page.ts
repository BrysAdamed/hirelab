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
import { Router, RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft, lucideCircleAlert, lucideLock, lucidePencil } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import { MarkdownViewer } from '@hirelab/ui/markdown';
import type { JobPosition } from '../api/staff-api';
import { AuthService } from '../auth/auth.service';
import { DeletePositionAction } from './delete-position-action';
import { PositionStateBadge } from './position-state-badge';
import { PositionsService } from './positions.service';

interface Section {
  title: string;
  markdown: string;
}

/** One job position: its Markdown sections, the manager's actions and its candidates. */
@Component({
  selector: 'hl-position-detail-page',
  imports: [
    DatePipe,
    RouterLink,
    NgIcon,
    HlmButton,
    HlmCardImports,
    MarkdownViewer,
    DeletePositionAction,
    PositionStateBadge,
  ],
  providers: [provideIcons({ lucideArrowLeft, lucideCircleAlert, lucideLock, lucidePencil })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a hlmBtn variant="link" size="sm" class="px-0" routerLink="/positions">
      <ng-icon name="lucideArrowLeft" aria-hidden="true" />
      Job positions
    </a>

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

    @if (position(); as position) {
      <div class="mt-2 flex flex-wrap items-start justify-between gap-2">
        <div class="flex flex-col gap-1">
          <div class="flex flex-wrap items-center gap-2">
            <h1 class="text-xl font-semibold">{{ position.title }}</h1>
            <hl-position-state-badge [state]="position.state" />
          </div>
          <p class="text-muted-foreground">Created {{ position.createdAt | date: 'mediumDate' }}</p>
        </div>
        @if (canChange()) {
          <div class="flex gap-2">
            <a hlmBtn variant="outline" size="sm" [routerLink]="['/positions', position.id, 'edit']">
              <ng-icon name="lucidePencil" aria-hidden="true" />
              Edit
            </a>
            <hl-delete-position-action
              [position]="position"
              (deleted)="deleted()"
              (failed)="error.set($event)"
            />
          </div>
        }
      </div>

      @if (position.state === 'Closed') {
        <p class="mt-3 flex items-center gap-2 rounded-md border p-2 text-muted-foreground">
          <ng-icon name="lucideLock" size="16" aria-hidden="true" />
          This position is closed and read-only.
        </p>
      }

      <section hlmCard class="mt-3" aria-label="Position details">
        <div hlmCardContent class="flex flex-col gap-4">
          @for (section of sections(); track section.title) {
            <section [attr.aria-labelledby]="sectionId(section)">
              <h2 class="font-semibold" [id]="sectionId(section)">{{ section.title }}</h2>
              <hl-markdown class="mt-1" [markdown]="section.markdown" />
            </section>
          }
        </div>
      </section>

      <section hlmCard class="mt-3" aria-labelledby="candidates-title">
        <div hlmCardHeader>
          <h2 hlmCardTitle id="candidates-title">Candidates</h2>
        </div>
        <div hlmCardContent>
          <!-- TODO HLB-41: the position's candidates table goes here. -->
          <p class="rounded-md border border-dashed p-3 text-center text-muted-foreground">
            No candidates yet.
          </p>
        </div>
      </section>
    }
  `,
})
export class PositionDetailPage {
  private readonly positionsService = inject(PositionsService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** The `:positionId` route parameter. */
  readonly positionId = input.required<string>();

  protected readonly position = signal<JobPosition | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly canChange = computed(
    () => this.auth.user()?.role === 'Manager' && this.position()?.state === 'Open',
  );
  protected readonly sections = computed<Section[]>(() => {
    const position = this.position();
    if (!position) {
      return [];
    }
    const sections: [string, string | null][] = [
      ['Description', position.description],
      ['Requirements', position.requirements],
      ['Nice-to-haves', position.niceToHaves],
      ['Interviewer notes', position.interviewerNotes],
    ];
    return sections.flatMap(([title, markdown]) => (markdown ? [{ title, markdown }] : []));
  });

  constructor() {
    effect(() => void this.load(this.positionId()));
  }

  protected sectionId(section: Section): string {
    return `section-${section.title.toLowerCase().replace(/[^a-z]+/g, '-')}`;
  }

  protected async deleted(): Promise<void> {
    await this.router.navigateByUrl('/positions');
  }

  private async load(positionId: string): Promise<void> {
    const result = await this.positionsService.get(positionId);
    if (result.ok) {
      this.error.set(null);
      this.position.set(result.value);
    } else {
      this.error.set(result.message);
      this.position.set(null);
    }
  }
}
