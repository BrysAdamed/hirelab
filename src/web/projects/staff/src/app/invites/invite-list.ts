import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideBan, lucideCircleCheck, lucideClock, lucideHourglass } from '@ng-icons/lucide';
import { HlmBadge } from '@hirelab/ui/helm/badge';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmDialogImports } from '@hirelab/ui/helm/dialog';
import type { Invite, InviteState } from '../api/staff-api';

const stateIcons: Record<InviteState, string> = {
  Pending: 'lucideClock',
  Used: 'lucideCircleCheck',
  Revoked: 'lucideBan',
  Expired: 'lucideHourglass',
};

/** The invites of a section with their state, and a confirmed Revoke action for pending ones. */
@Component({
  selector: 'hl-invite-list',
  imports: [DatePipe, NgIcon, HlmBadge, HlmButton, HlmDialogImports],
  providers: [provideIcons({ lucideBan, lucideCircleCheck, lucideClock, lucideHourglass })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (invites().length === 0) {
      <p class="rounded-md border border-dashed p-3 text-center text-muted-foreground">
        No invites yet.
      </p>
    } @else {
      <div class="overflow-x-auto rounded-md border">
        <table class="w-full text-left">
          <caption class="sr-only">
            {{
              caption()
            }}
          </caption>
          <thead class="border-b bg-muted text-muted-foreground">
            <tr>
              <th scope="col" class="px-3 py-2 font-medium">Invitee</th>
              <th scope="col" class="px-3 py-2 font-medium">Role</th>
              <th scope="col" class="px-3 py-2 font-medium">Team</th>
              <th scope="col" class="px-3 py-2 font-medium">Expires</th>
              <th scope="col" class="px-3 py-2 font-medium">State</th>
              <th scope="col" class="px-3 py-2"><span class="sr-only">Actions</span></th>
            </tr>
          </thead>
          <tbody>
            @for (invite of invites(); track invite.id) {
              <tr class="border-b last:border-b-0">
                <td class="px-3 py-2">
                  <div>{{ invite.displayName }}</div>
                  <div class="text-muted-foreground">{{ invite.email }}</div>
                </td>
                <td class="px-3 py-2">{{ invite.role }}</td>
                <td class="px-3 py-2">
                  @if (invite.team; as team) {
                    {{ team.name }}
                    @if (team.isNew) {
                      <span class="text-muted-foreground">(new team)</span>
                    }
                  }
                </td>
                <td class="px-3 py-2">{{ invite.expiresAt | date: 'medium' }}</td>
                <td class="px-3 py-2">
                  <span hlmBadge [variant]="invite.state === 'Pending' ? 'secondary' : 'outline'">
                    <ng-icon [name]="stateIcons[invite.state]" aria-hidden="true" />
                    {{ invite.state }}
                  </span>
                </td>
                <td class="px-3 py-2 text-right">
                  @if (invite.state === 'Pending') {
                    <hlm-dialog>
                      <button
                        hlmDialogTrigger
                        hlmBtn
                        type="button"
                        variant="outline"
                        size="sm"
                        [attr.aria-label]="'Revoke the invite for ' + invite.email"
                      >
                        Revoke
                      </button>
                      <ng-template hlmDialogPortal>
                        <hlm-dialog-content>
                          <hlm-dialog-header>
                            <h3 hlmDialogTitle>Revoke invite</h3>
                            <p hlmDialogDescription>
                              The link sent to {{ invite.email }} stops working. You can issue a new
                              invite later.
                            </p>
                          </hlm-dialog-header>
                          <hlm-dialog-footer>
                            <button hlmBtn hlmDialogClose type="button" variant="outline">
                              Cancel
                            </button>
                            <button
                              hlmBtn
                              hlmDialogClose
                              type="button"
                              variant="destructive"
                              (click)="revoke.emit(invite)"
                            >
                              Revoke invite
                            </button>
                          </hlm-dialog-footer>
                        </hlm-dialog-content>
                      </ng-template>
                    </hlm-dialog>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
})
export class InviteList {
  readonly invites = input.required<readonly Invite[]>();
  readonly caption = input.required<string>();
  readonly revoke = output<Invite>();

  protected readonly stateIcons = stateIcons;
}
