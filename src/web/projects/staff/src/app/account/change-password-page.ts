import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert, lucideCircleCheck, lucideLoaderCircle } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import { HlmInput } from '@hirelab/ui/helm/input';
import { HlmLabel } from '@hirelab/ui/helm/label';
import { AuthService } from '../auth/auth.service';

const minimumPasswordLength = 12;

/** The signed-in user changes their own password, opened from the user menu. */
@Component({
  selector: 'hl-change-password-page',
  imports: [ReactiveFormsModule, NgIcon, HlmButton, HlmCardImports, HlmInput, HlmLabel],
  providers: [provideIcons({ lucideCircleAlert, lucideCircleCheck, lucideLoaderCircle })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="text-xl font-semibold">Change password</h1>
    <section hlmCard class="mt-3 w-full max-w-md" aria-labelledby="change-password-title">
      <div hlmCardHeader>
        <h2 hlmCardTitle id="change-password-title">Your password</h2>
        <p hlmCardDescription>
          You stay logged in here. Your sessions in other browsers end within a few minutes.
        </p>
      </div>
      <div hlmCardContent class="flex flex-col gap-3">
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
        @if (changed()) {
          <div role="status" class="flex items-start gap-2 rounded-md border p-2">
            <ng-icon
              name="lucideCircleCheck"
              size="16"
              class="mt-0.5 shrink-0"
              aria-hidden="true"
            />
            <p>Your password is changed.</p>
          </div>
        }
        <form class="flex flex-col gap-3" [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="flex flex-col gap-1.5">
            <label hlmLabel for="current-password">Current password</label>
            <input
              hlmInput
              id="current-password"
              type="password"
              autocomplete="current-password"
              formControlName="currentPassword"
              required
            />
          </div>
          <div class="flex flex-col gap-1.5">
            <label hlmLabel for="new-password">New password</label>
            <input
              hlmInput
              id="new-password"
              type="password"
              autocomplete="new-password"
              formControlName="newPassword"
              aria-describedby="new-password-hint"
              required
            />
            <p id="new-password-hint" class="text-muted-foreground">
              At least {{ minimumPasswordLength }} characters.
            </p>
          </div>
          <div class="flex flex-col gap-1.5">
            <label hlmLabel for="confirm-new-password">Confirm new password</label>
            <input
              hlmInput
              id="confirm-new-password"
              type="password"
              autocomplete="new-password"
              formControlName="confirmPassword"
              required
            />
          </div>
          <div>
            <button hlmBtn type="submit" [disabled]="pending()">
              @if (pending()) {
                <ng-icon
                  name="lucideLoaderCircle"
                  class="motion-safe:animate-spin"
                  aria-hidden="true"
                />
              }
              Change password
            </button>
          </div>
        </form>
      </div>
    </section>
  `,
})
export class ChangePasswordPage {
  private readonly auth = inject(AuthService);

  protected readonly minimumPasswordLength = minimumPasswordLength;
  protected readonly pending = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly changed = signal(false);

  protected readonly form = inject(NonNullableFormBuilder).group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(minimumPasswordLength)]],
    confirmPassword: ['', Validators.required],
  });

  protected async submit(): Promise<void> {
    if (this.pending()) {
      return;
    }
    const { currentPassword, newPassword, confirmPassword } = this.form.getRawValue();
    this.changed.set(false);
    if (this.form.controls.currentPassword.invalid) {
      this.error.set('Enter your current password.');
      return;
    }
    if (this.form.controls.newPassword.invalid) {
      this.error.set(`Choose a new password of at least ${minimumPasswordLength} characters.`);
      return;
    }
    if (newPassword !== confirmPassword) {
      this.error.set('The new passwords don’t match.');
      return;
    }

    this.pending.set(true);
    this.error.set(null);
    const result = await this.auth.changePassword(currentPassword, newPassword);
    this.pending.set(false);
    this.form.reset();

    if (result.ok) {
      this.changed.set(true);
    } else {
      this.error.set(result.message);
    }
  }
}
