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
import { type ApiFailure, type ApiResult, changedMeanwhileCode } from '../api/api-result';
import type { Candidate, CandidateDetails } from '../api/staff-api';
import { CandidatesService } from './candidates.service';

const fullNameMaxLength = 200;
const emailMaxLength = 254;
const phoneMaxLength = 50;
const linkMaxLength = 500;
const linkSlots = [1, 2, 3] as const;

/** Whether a profile link is a full http or https address, as the API requires. */
export function isProfileLink(link: string): boolean {
  try {
    const url = new URL(link);
    return (url.protocol === 'https:' || url.protocol === 'http:') && url.hostname !== '';
  } catch {
    return false;
  }
}

/**
 * Adds a candidate to an open position (`/positions/:positionId/candidates/new`) or edits one
 * (`/candidates/:candidateId/edit`). An edit is based on the version the form was loaded with; when someone else saved
 * the candidate since, the page offers to reload them.
 */
@Component({
  selector: 'hl-candidate-form-page',
  imports: [ReactiveFormsModule, RouterLink, NgIcon, HlmButton, HlmCardImports, HlmInput, HlmLabel],
  providers: [provideIcons({ lucideCircleAlert, lucideLoaderCircle, lucideRefreshCw })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="text-xl font-semibold">{{ isEdit() ? 'Edit candidate' : 'Add candidate' }}</h1>

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
              Reload candidate
            </button>
          }
        </div>
      </div>
    }

    @if (!isEdit() || loaded()) {
      <section hlmCard class="mt-3 w-full max-w-2xl" aria-label="Candidate">
        <div hlmCardContent>
          <form class="flex flex-col gap-4" [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <div class="flex flex-col gap-1.5">
              <label hlmLabel for="candidate-full-name">Full name</label>
              <input
                hlmInput
                id="candidate-full-name"
                autocomplete="off"
                formControlName="fullName"
                required
                [attr.maxlength]="fullNameMaxLength"
              />
            </div>
            <div class="flex flex-col gap-1.5">
              <label hlmLabel for="candidate-email">
                Email <span class="font-normal text-muted-foreground">(optional)</span>
              </label>
              <input
                hlmInput
                id="candidate-email"
                type="email"
                autocomplete="off"
                formControlName="email"
                [attr.maxlength]="emailMaxLength"
              />
            </div>
            <div class="flex flex-col gap-1.5">
              <label hlmLabel for="candidate-phone">
                Phone <span class="font-normal text-muted-foreground">(optional)</span>
              </label>
              <input
                hlmInput
                id="candidate-phone"
                type="tel"
                autocomplete="off"
                formControlName="phone"
                [attr.maxlength]="phoneMaxLength"
              />
            </div>
            <fieldset class="flex flex-col gap-2" aria-describedby="candidate-links-hint">
              <legend class="text-sm font-medium">
                Profile links <span class="font-normal text-muted-foreground">(optional)</span>
              </legend>
              <p id="candidate-links-hint" class="text-muted-foreground">
                Up to three full addresses, such as a LinkedIn or GitHub profile.
              </p>
              @for (slot of linkSlots; track slot) {
                <div class="flex flex-col gap-1.5">
                  <label hlmLabel class="sr-only" [for]="'candidate-link-' + slot">
                    Profile link {{ slot }}
                  </label>
                  <input
                    hlmInput
                    type="url"
                    autocomplete="off"
                    placeholder="https://"
                    [id]="'candidate-link-' + slot"
                    [formControlName]="'link' + slot"
                    [attr.maxlength]="linkMaxLength"
                  />
                </div>
              }
            </fieldset>
            <div class="flex gap-2">
              <button hlmBtn type="submit" [disabled]="pending()">
                @if (pending()) {
                  <ng-icon
                    name="lucideLoaderCircle"
                    class="motion-safe:animate-spin"
                    aria-hidden="true"
                  />
                }
                {{ isEdit() ? 'Save changes' : 'Add candidate' }}
              </button>
              <a hlmBtn variant="outline" [routerLink]="cancelLink()">Cancel</a>
            </div>
          </form>
        </div>
      </section>
    }
  `,
})
export class CandidateFormPage {
  private readonly candidatesService = inject(CandidatesService);
  private readonly router = inject(Router);

  /** The `:positionId` route parameter when adding a candidate. */
  readonly positionId = input<string | undefined>();
  /** The `:candidateId` route parameter when editing one. */
  readonly candidateId = input<string | undefined>();

  protected readonly fullNameMaxLength = fullNameMaxLength;
  protected readonly emailMaxLength = emailMaxLength;
  protected readonly phoneMaxLength = phoneMaxLength;
  protected readonly linkMaxLength = linkMaxLength;
  protected readonly linkSlots = linkSlots;
  protected readonly isEdit = computed(() => !!this.candidateId());
  protected readonly cancelLink = computed(() => {
    const candidateId = this.candidateId();
    return candidateId ? ['/candidates', candidateId] : ['/positions', this.positionId() ?? ''];
  });
  protected readonly loaded = signal(false);
  protected readonly pending = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly changedMeanwhile = signal(false);

  private version = 0;

  protected readonly form = inject(NonNullableFormBuilder).group({
    fullName: ['', [Validators.required, Validators.maxLength(fullNameMaxLength)]],
    email: ['', Validators.maxLength(emailMaxLength)],
    phone: ['', Validators.maxLength(phoneMaxLength)],
    link1: ['', Validators.maxLength(linkMaxLength)],
    link2: ['', Validators.maxLength(linkMaxLength)],
    link3: ['', Validators.maxLength(linkMaxLength)],
  });

  constructor() {
    effect(() => {
      const candidateId = this.candidateId();
      if (candidateId) {
        void this.load(candidateId);
      }
    });
  }

  protected async reload(): Promise<void> {
    const candidateId = this.candidateId();
    if (candidateId) {
      await this.load(candidateId);
    }
  }

  protected async submit(): Promise<void> {
    if (this.pending()) {
      return;
    }
    const details = this.details();
    const problem = this.problemWith(details);
    if (problem) {
      this.showError(problem);
      return;
    }

    this.pending.set(true);
    this.showError(null);
    const result = await this.save(details);
    this.pending.set(false);

    if (!result.ok) {
      this.saveFailed(result);
      return;
    }

    await this.router.navigate(['/candidates', result.value]);
  }

  /** Adds or edits the candidate and returns their id. */
  private async save(details: CandidateDetails): Promise<ApiResult<string>> {
    const candidateId = this.candidateId();
    if (candidateId) {
      const edited = await this.candidatesService.edit(candidateId, details, this.version);
      return edited.ok ? { ok: true, value: candidateId } : edited;
    }
    const added = await this.candidatesService.add(this.positionId() ?? '', details);
    return added.ok ? { ok: true, value: added.value.id } : added;
  }

  private saveFailed(failure: ApiFailure): void {
    const changedMeanwhile = failure.code === changedMeanwhileCode;
    this.showError(
      changedMeanwhile
        ? 'Someone else changed this candidate while you were editing them. Reload to see their changes; your edits here will be lost.'
        : failure.message,
      changedMeanwhile,
    );
  }

  private details(): CandidateDetails {
    const value = this.form.getRawValue();
    return {
      fullName: value.fullName.trim(),
      email: value.email.trim() || null,
      phone: value.phone.trim() || null,
      links: [value.link1, value.link2, value.link3]
        .map((link) => link.trim())
        .filter((link) => link !== ''),
    };
  }

  private problemWith(details: CandidateDetails): string | null {
    if (!details.fullName) {
      return "Enter the candidate's full name.";
    }
    if (details.email && !/^[^\s@]+@[^\s@]+$/.test(details.email)) {
      return 'Enter a valid email address.';
    }
    if (details.links?.some((link) => !isProfileLink(link))) {
      return 'Enter each profile link as a full http or https address.';
    }
    return null;
  }

  private async load(candidateId: string): Promise<void> {
    const result = await this.candidatesService.get(candidateId);
    if (!result.ok) {
      this.showError(result.message);
      return;
    }
    const candidate = result.value;
    const readOnly =
      candidate.positionState === 'Closed'
        ? 'This candidate’s position is closed, so their details are read-only.'
        : candidate.isAnonymised
          ? 'This candidate has been anonymised, so their details can no longer be changed.'
          : null;
    if (readOnly) {
      this.loaded.set(false);
      this.showError(readOnly);
      return;
    }
    this.fill(candidate);
    this.showError(null);
    this.loaded.set(true);
  }

  private fill(candidate: Candidate): void {
    this.version = candidate.version;
    const [link1 = '', link2 = '', link3 = ''] = candidate.links;
    this.form.reset({
      fullName: candidate.fullName ?? '',
      email: candidate.email ?? '',
      phone: candidate.phone ?? '',
      link1,
      link2,
      link3,
    });
  }

  private showError(message: string | null, changedMeanwhile = false): void {
    this.error.set(message);
    this.changedMeanwhile.set(changedMeanwhile);
  }
}
