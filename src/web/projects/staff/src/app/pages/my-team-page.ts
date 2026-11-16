import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert } from '@ng-icons/lucide';
import type { UserSummary } from '../api/staff-api';
import { AuthService } from '../auth/auth.service';
import { InvitesSection } from '../invites/invites-section';
import { ResetLinkAction } from '../reset-links/reset-link-action';
import { UserActivationAction } from '../users/user-activation-action';
import { UserRowActions, UserTable } from '../users/user-table';
import { UsersService } from '../users/users.service';

/** The manager's My team page: the team's interviewers and the interviewer invites. */
@Component({
  selector: 'hl-my-team-page',
  imports: [
    NgIcon,
    InvitesSection,
    ResetLinkAction,
    UserActivationAction,
    UserRowActions,
    UserTable,
  ],
  providers: [provideIcons({ lucideCircleAlert })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="text-xl font-semibold">My team</h1>
    @if (auth.user()?.team; as team) {
      <p class="mt-1 text-muted-foreground">{{ team.name }}</p>
    }

    <section class="mt-3 flex flex-col gap-2" aria-labelledby="my-team-interviewers-title">
      <h2 class="font-semibold" id="my-team-interviewers-title">Interviewers</h2>
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
      @if (loaded()) {
        <hl-user-table
          caption="Interviewers"
          emptyText="No interviewers yet. Invite one below."
          [users]="interviewers()"
        >
          <ng-template hlUserRowActions let-user>
            <hl-user-activation-action
              scope="my-team"
              [user]="user"
              (changed)="refresh()"
              (failed)="error.set($event)"
            />
            @if (user.isActive) {
              <hl-reset-link-action
                kind="interviewers"
                [userId]="user.id"
                [displayName]="user.displayName"
              />
            }
          </ng-template>
        </hl-user-table>
      }
    </section>

    <hl-invites-section class="mt-6" kind="interviewers" />
  `,
})
export class MyTeamPage implements OnInit {
  private readonly usersService = inject(UsersService);
  protected readonly auth = inject(AuthService);

  protected readonly interviewers = signal<UserSummary[]>([]);
  protected readonly loaded = signal(false);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    void this.refresh();
  }

  protected async refresh(): Promise<void> {
    const result = await this.usersService.list('my-team');
    this.loaded.set(true);
    if (result.ok) {
      this.error.set(null);
      this.interviewers.set(result.value);
    } else {
      this.error.set(result.message);
    }
  }
}
