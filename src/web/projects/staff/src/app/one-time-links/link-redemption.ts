import { computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import type { ApiResult } from '../api/api-result';

const minimumPasswordLength = 12;

/** The endpoints a one-time link page needs: an antiforgery token, the link's preview and its redemption. */
export interface LinkRedemptionApi<TPreview> {
  prepareRedemption(): Promise<void>;
  preview(token: string): Promise<ApiResult<TPreview>>;
  redeem(token: string, password: string): Promise<ApiResult<unknown>>;
}

type LinkState<TPreview> =
  | { kind: 'loading' }
  | { kind: 'unavailable'; message: string }
  | { kind: 'ready'; preview: TPreview };

/**
 * The shared part of a one-time link page (`/invite#<token>`, `/reset#<token>`): it reads the token from the fragment,
 * previews the link and redeems it with a new password. Create it in a component field initialiser.
 */
export class LinkRedemption<TPreview> {
  readonly minimumPasswordLength = minimumPasswordLength;

  private readonly token = inject(ActivatedRoute).snapshot.fragment ?? '';
  private readonly state = signal<LinkState<TPreview>>({ kind: 'loading' });

  readonly loading = computed(() => this.state().kind === 'loading');
  readonly preview = computed(() => {
    const state = this.state();
    return state.kind === 'ready' ? state.preview : null;
  });
  readonly unavailable = computed(() => {
    const state = this.state();
    return state.kind === 'unavailable' ? state.message : null;
  });
  readonly pending = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = inject(NonNullableFormBuilder).group({
    password: ['', [Validators.required, Validators.minLength(minimumPasswordLength)]],
    confirmPassword: ['', Validators.required],
  });

  /** @param linkName How the page calls the link, for example `invite link`. */
  constructor(
    private readonly api: LinkRedemptionApi<TPreview>,
    private readonly linkName: string,
  ) {}

  async load(): Promise<void> {
    if (!this.token) {
      this.state.set({
        kind: 'unavailable',
        message: `This ${this.linkName} is incomplete. Check that you copied the whole link.`,
      });
      return;
    }

    await this.api.prepareRedemption();
    const preview = await this.api.preview(this.token);
    this.state.set(
      preview.ok
        ? { kind: 'ready', preview: preview.value }
        : { kind: 'unavailable', message: preview.message },
    );
  }

  /** Checks the password and redeems the link; `true` once it is redeemed. */
  async submit(): Promise<boolean> {
    if (this.pending()) {
      return false;
    }
    const { password, confirmPassword } = this.form.getRawValue();
    if (this.form.controls.password.invalid) {
      this.error.set(`Choose a password of at least ${minimumPasswordLength} characters.`);
      return false;
    }
    if (password !== confirmPassword) {
      this.error.set('The passwords don’t match.');
      return false;
    }

    this.pending.set(true);
    this.error.set(null);
    const result = await this.api.redeem(this.token, password);
    this.pending.set(false);

    if (!result.ok) {
      this.error.set(result.message);
      this.form.reset();
    }
    return result.ok;
  }
}
