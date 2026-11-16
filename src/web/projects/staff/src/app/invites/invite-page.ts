import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
  type OnInit,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert, lucideLoaderCircle } from '@ng-icons/lucide';
import { ThemeSwitcher } from '@hirelab/ui';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import { HlmInput } from '@hirelab/ui/helm/input';
import { HlmLabel } from '@hirelab/ui/helm/label';
import type { InvitePreview } from '../api/staff-api';
import { InvitesService } from './invites.service';

const minimumPasswordLength = 12;

type PageState =
  | { kind: 'loading' }
  | { kind: 'unavailable'; message: string }
  | { kind: 'ready'; invite: InvitePreview };

/**
 * Redeems an invite link (`/invite#<token>`). Anonymous and outside the app shell: it shows the role and team, the
 * invitee sets a password, and the API creates the account and signs them in.
 */
@Component({
  selector: 'hl-invite-page',
  imports: [
    ReactiveFormsModule,
    NgIcon,
    ThemeSwitcher,
    HlmButton,
    HlmCardImports,
    HlmInput,
    HlmLabel,
  ],
  providers: [provideIcons({ lucideCircleAlert, lucideLoaderCircle })],
  templateUrl: './invite-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InvitePage implements OnInit {
  private readonly invites = inject(InvitesService);
  private readonly router = inject(Router);
  private readonly token = inject(ActivatedRoute).snapshot.fragment ?? '';

  protected readonly minimumPasswordLength = minimumPasswordLength;
  private readonly state = signal<PageState>({ kind: 'loading' });
  protected readonly loading = computed(() => this.state().kind === 'loading');
  protected readonly invite = computed(() => {
    const state = this.state();
    return state.kind === 'ready' ? state.invite : null;
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
        message: 'This invite link is incomplete. Check that you copied the whole link.',
      });
      return;
    }

    await this.invites.prepareRedemption();
    const preview = await this.invites.preview(this.token);
    this.state.set(
      preview.ok
        ? { kind: 'ready', invite: preview.value }
        : { kind: 'unavailable', message: preview.message },
    );
  }

  protected roleAndTeam(invite: InvitePreview): string {
    const role = invite.role.toLowerCase();
    if (!invite.teamName) {
      return `as the ${role}`;
    }
    return invite.isNewTeam
      ? `as the ${role} of a new team, ${invite.teamName}`
      : `as ${role === 'interviewer' ? 'an' : 'the'} ${role} in ${invite.teamName}`;
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
    const result = await this.invites.redeem(this.token, password);
    this.pending.set(false);

    if (result.ok) {
      await this.router.navigateByUrl('/');
    } else {
      this.error.set(result.message);
      this.form.reset();
    }
  }
}
