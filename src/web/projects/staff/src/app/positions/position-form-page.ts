import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert, lucideLoaderCircle, lucideRefreshCw } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import { HlmInput } from '@hirelab/ui/helm/input';
import { HlmLabel } from '@hirelab/ui/helm/label';
import { HlmTextarea } from '@hirelab/ui/helm/textarea';
import { changedMeanwhileCode } from '../api/api-result';
import type { JobPosition, JobPositionDetails } from '../api/staff-api';
import { PositionsService } from './positions.service';

const titleMaxLength = 200;
const sectionMaxLength = 20_000;

interface SectionField {
  name: 'description' | 'requirements' | 'niceToHaves' | 'interviewerNotes';
  label: string;
  required: boolean;
  hint: string;
}

const sectionFields: readonly SectionField[] = [
  {
    name: 'description',
    label: 'Description',
    required: false,
    hint: 'What the role is about. Markdown.',
  },
  {
    name: 'requirements',
    label: 'Requirements',
    required: true,
    hint: 'What a candidate must bring. Markdown.',
  },
  {
    name: 'niceToHaves',
    label: 'Nice-to-haves',
    required: false,
    hint: 'What would be a plus. Markdown.',
  },
  {
    name: 'interviewerNotes',
    label: 'Interviewer notes',
    required: false,
    hint: 'For your team only, never shown to candidates. Markdown.',
  },
];

/**
 * Creates a job position (`/positions/new`) or edits an open one (`/positions/:positionId/edit`). An edit is based on
 * the version the form was loaded with; when someone else saved the position since, the page offers to reload it.
 */
@Component({
  selector: 'hl-position-form-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    NgIcon,
    HlmButton,
    HlmCardImports,
    HlmInput,
    HlmLabel,
    HlmTextarea,
  ],
  providers: [provideIcons({ lucideCircleAlert, lucideLoaderCircle, lucideRefreshCw })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="text-xl font-semibold">{{ isEdit() ? 'Edit job position' : 'New job position' }}</h1>

    @if (error(); as message) {
      <div role="alert" class="mt-3 flex items-start gap-2 rounded-md border border-destructive p-2">
        <ng-icon
          name="lucideCircleAlert"
          size="16"
          class="mt-0.5 shrink-0 text-destructive"
          aria-hidden="true"
        />
        <div class="flex flex-col items-start gap-2">
          <p>{{ message }}</p>
          @if (changedMeanwhile()) {
            <button hlmBtn type="button" variant="outline" size="sm" (click)="reload()">
              <ng-icon name="lucideRefreshCw" aria-hidden="true" />
              Reload position
            </button>
          }
        </div>
      </div>
    }

    @if (!isEdit() || loaded()) {
      <section hlmCard class="mt-3 w-full max-w-3xl" aria-label="Job position">
        <div hlmCardContent>
          <form class="flex flex-col gap-4" [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <div class="flex flex-col gap-1.5">
              <label hlmLabel for="position-title">Title</label>
              <input
                hlmInput
                id="position-title"
                autocomplete="off"
                formControlName="title"
                required
                [attr.maxlength]="titleMaxLength"
              />
            </div>
            @for (field of sectionFields; track field.name) {
              <div class="flex flex-col gap-1.5">
                <label hlmLabel [for]="'position-' + field.name">
                  {{ field.label }}
                  @if (!field.required) {
                    <span class="font-normal text-muted-foreground">(optional)</span>
                  }
                </label>
                <textarea
                  hlmTextarea
                  class="min-h-24 font-mono"
                  [id]="'position-' + field.name"
                  [formControlName]="field.name"
                  [required]="field.required"
                  [attr.maxlength]="sectionMaxLength"
                  [aria-describedby]="'position-' + field.name + '-hint'"
                ></textarea>
                <p [id]="'position-' + field.name + '-hint'" class="text-muted-foreground">
                  {{ field.hint }}
                </p>
              </div>
            }
            <div class="flex gap-2">
              <button hlmBtn type="submit" [disabled]="pending()">
                @if (pending()) {
                  <ng-icon
                    name="lucideLoaderCircle"
                    class="motion-safe:animate-spin"
                    aria-hidden="true"
                  />
                }
                {{ isEdit() ? 'Save changes' : 'Create position' }}
              </button>
              <a hlmBtn variant="outline" [routerLink]="cancelLink()">Cancel</a>
            </div>
          </form>
        </div>
      </section>
    }
  `,
})
export class PositionFormPage {
  private readonly positionsService = inject(PositionsService);
  private readonly router = inject(Router);

  /** The `:positionId` route parameter; absent when creating a position. */
  readonly positionId = input<string | undefined>();

  protected readonly titleMaxLength = titleMaxLength;
  protected readonly sectionMaxLength = sectionMaxLength;
  protected readonly sectionFields = sectionFields;
  protected readonly isEdit = computed(() => !!this.positionId());
  protected readonly cancelLink = computed(() => {
    const positionId = this.positionId();
    return positionId ? ['/positions', positionId] : ['/positions'];
  });
  protected readonly loaded = signal(false);
  protected readonly pending = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly changedMeanwhile = signal(false);

  private version = 0;

  protected readonly form = inject(NonNullableFormBuilder).group({
    title: ['', [Validators.required, Validators.maxLength(titleMaxLength)]],
    description: ['', Validators.maxLength(sectionMaxLength)],
    requirements: ['', [Validators.required, Validators.maxLength(sectionMaxLength)]],
    niceToHaves: ['', Validators.maxLength(sectionMaxLength)],
    interviewerNotes: ['', Validators.maxLength(sectionMaxLength)],
  });

  constructor() {
    effect(() => {
      const positionId = this.positionId();
      if (positionId) {
        void this.load(positionId);
      }
    });
  }

  protected async reload(): Promise<void> {
    const positionId = this.positionId();
    if (positionId) {
      await this.load(positionId);
    }
  }

  protected async submit(): Promise<void> {
    if (this.pending()) {
      return;
    }
    const details = this.details();
    if (!details.title) {
      this.showError('Enter a title.');
      return;
    }
    if (!details.requirements.trim()) {
      this.showError('Enter the requirements.');
      return;
    }

    this.pending.set(true);
    this.showError(null);
    const positionId = this.positionId();
    const result = positionId
      ? await this.positionsService.edit(positionId, details, this.version)
      : await this.positionsService.create(details);
    this.pending.set(false);

    if (!result.ok) {
      const changedMeanwhile = result.code === changedMeanwhileCode;
      this.showError(
        changedMeanwhile
          ? 'Someone else changed this position while you were editing it. Reload it to see their changes; your edits here will be lost.'
          : result.message,
        changedMeanwhile,
      );
      return;
    }

    await this.router.navigate(['/positions', positionId ?? result.value]);
  }

  private details(): JobPositionDetails {
    const value = this.form.getRawValue();
    return {
      title: value.title.trim(),
      description: value.description || null,
      requirements: value.requirements,
      niceToHaves: value.niceToHaves || null,
      interviewerNotes: value.interviewerNotes || null,
    };
  }

  private async load(positionId: string): Promise<void> {
    const result = await this.positionsService.get(positionId);
    if (!result.ok) {
      this.showError(result.message);
      return;
    }
    if (result.value.state === 'Closed') {
      this.loaded.set(false);
      this.showError('This position is closed and read-only.');
      return;
    }
    this.fill(result.value);
    this.showError(null);
    this.loaded.set(true);
  }

  private fill(position: JobPosition): void {
    this.version = position.version;
    this.form.reset({
      title: position.title,
      description: position.description ?? '',
      requirements: position.requirements,
      niceToHaves: position.niceToHaves ?? '',
      interviewerNotes: position.interviewerNotes ?? '',
    });
  }

  private showError(message: string | null, changedMeanwhile = false): void {
    this.error.set(message);
    this.changedMeanwhile.set(changedMeanwhile);
  }
}
