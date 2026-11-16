import { ChangeDetectionStrategy, Component, inject, type OnInit } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert, lucideLoaderCircle } from '@ng-icons/lucide';
import { ThemeSwitcher } from '@hirelab/ui';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import { HlmInput } from '@hirelab/ui/helm/input';
import { HlmLabel } from '@hirelab/ui/helm/label';
import type { InvitePreview } from '../api/staff-api';
import { LinkRedemption } from '../one-time-links/link-redemption';
import { InvitesService } from './invites.service';

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
  private readonly router = inject(Router);
  private readonly redemption = new LinkRedemption(inject(InvitesService), 'invite link');

  protected readonly minimumPasswordLength = this.redemption.minimumPasswordLength;
  protected readonly loading = this.redemption.loading;
  protected readonly invite = this.redemption.preview;
  protected readonly unavailable = this.redemption.unavailable;
  protected readonly pending = this.redemption.pending;
  protected readonly error = this.redemption.error;
  protected readonly form = this.redemption.form;

  async ngOnInit(): Promise<void> {
    await this.redemption.load();
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
    if (await this.redemption.submit()) {
      await this.router.navigateByUrl('/');
    }
  }
}
