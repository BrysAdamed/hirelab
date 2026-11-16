import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { InvitesSection } from '../invites/invites-section';

/** The manager's My team page. Managing the team's interviewers (HLB-37) joins the interviewer invites here. */
@Component({
  selector: 'hl-my-team-page',
  imports: [InvitesSection],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="text-xl font-semibold">My team</h1>
    @if (auth.user()?.team; as team) {
      <p class="mt-1 text-muted-foreground">{{ team.name }}</p>
    }
    <hl-invites-section class="mt-3" kind="interviewers" />
  `,
})
export class MyTeamPage {
  protected readonly auth = inject(AuthService);
}
