import { ChangeDetectionStrategy, Component } from '@angular/core';
import { InvitesSection } from '../invites/invites-section';

/** The admin's Teams page. Team management (HLB-37) joins the manager invites here. */
@Component({
  selector: 'hl-teams-page',
  imports: [InvitesSection],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="text-xl font-semibold">Teams</h1>
    <hl-invites-section class="mt-3" kind="managers" />
  `,
})
export class TeamsPage {}
