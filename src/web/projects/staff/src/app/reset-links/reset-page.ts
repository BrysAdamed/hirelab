import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
  type OnInit,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert, lucideCircleCheck, lucideLoaderCircle } from '@ng-icons/lucide';
import { ThemeSwitcher } from '@hirelab/ui';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import { HlmInput } from '@hirelab/ui/helm/input';
import { HlmLabel } from '@hirelab/ui/helm/label';
import type { ResetLinkPreview } from '../api/staff-api';
import { ResetLinksService } from './reset-links.service';

const minimumPasswordLength = 12;

type PageState =
  | { kind: 'loading' }
  | { kind: 'unavailable'; message: string }
  | { kind: 'ready'; link: ResetLinkPreview }
  | { kind: 'done' };

/**
 * Redeems a reset link (`/reset#<token>`). Anonymous and outside the app shell: the user sets a new password, which
 * ends their other sessions, and then logs in with it.
 */
@Component({
  selector: 'hl-reset-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    NgIcon,
    ThemeSwitcher,
    HlmButton,
    HlmCardImports,
    HlmInput,
    HlmLabel,
  ],
  providers: [provideIcons({ lucideCircleAlert, lucideCircleCheck, lucideLoaderCircle })],
  templateUrl: './reset-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResetPage implements OnInit {
  private readonly resetLinks = inject(ResetLinksService);
  private readonly token = inject(ActivatedRoute).snapshot.fragment ?? '';

  protected readonly minimumPasswordLength = minimumPasswordLength;
  private readonly state = signal<PageState>({ kind: 'loading' });
  protected readonly loading = computed(() => this.state().kind === 'loading');
  protected readonly done = computed(() => this.state().kind === 'done');
  protected readonly link = computed(() => {
    const state = this.state();
    return state.kind === 'ready' ? state.link : null;
  });
  protected readonly unavailable = computed(() => {
    const state = this.state();
    return state.kind === 'unavailable' ? state.message : null;
  });
  protected readonly pending = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    password: ['', [Validators.required, Validators.minLength(minimumPasswordLength)]],
    confirmPassword: ['', Validators.required],
  });

  async ngOnInit(): Promise<void> {
    if (!this.token) {
      this.state.set({
        kind: 'unavailable',
        message: 'This reset link is incomplete. Check that you copied the whole link.',
      });
      return;
    }

    await this.resetLinks.prepareRedemption();
    const preview = await this.resetLinks.preview(this.token);
    this.state.set(
      preview.ok
        ? { kind: 'ready', link: preview.value }
        : { kind: 'unavailable', message: preview.message },
    );
  }

  protected async submit(): Promise<void> {
    if (this.pending()) {
      return;
    }
    const { password, confirmPassword } = this.form.getRawValue();
    if (this.form.controls.password.invalid) {
      this.error.set(`Choose a password of at least ${minimumPasswordLength} characters.`);
      return;
    }
    if (password !== confirmPassword) {
      this.error.set('The passwords don’t match.');
      return;
    }

    this.pending.set(true);
    this.error.set(null);
    const result = await this.resetLinks.redeem(this.token, password);
    this.pending.set(false);

    if (result.ok) {
      this.state.set({ kind: 'done' });
    } else {
      this.error.set(result.message);
      this.form.reset();
    }
  }
}
