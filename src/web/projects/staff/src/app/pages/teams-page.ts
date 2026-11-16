import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
  type OnInit,
} from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert, lucideUserX } from '@ng-icons/lucide';
import { HlmBadge } from '@hirelab/ui/helm/badge';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import type { TeamSummary } from '../api/staff-api';
import { InvitesSection } from '../invites/invites-section';
import { RenameTeamAction } from '../teams/rename-team-action';
import { ReassignAction } from '../users/reassign-action';
import { UserActivationAction } from '../users/user-activation-action';
import { UserRowActions, UserTable } from '../users/user-table';
import { UsersService } from '../users/users.service';

/** The admin's Teams page: every team with its manager and members, and the manager invites. */
@Component({
  selector: 'hl-teams-page',
  imports: [
    NgIcon,
    HlmBadge,
    HlmCardImports,
    InvitesSection,
    RenameTeamAction,
    ReassignAction,
    UserActivationAction,
    UserRowActions,
    UserTable,
  ],
  providers: [provideIcons({ lucideCircleAlert, lucideUserX })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="text-xl font-semibold">Teams</h1>

    @if (error(); as message) {
      <div
        role="alert"
        class="mt-3 flex items-start gap-2 rounded-md border border-destructive p-2"
      >
        <ng-icon
          name="lucideCircleAlert"
          size="16"
          class="mt-0.5 shrink-0 text-destructive"
          aria-hidden="true"
        />
        <p>{{ message }}</p>
      </div>
    }

    <div class="mt-3 flex flex-col gap-3">
      @for (team of teams(); track team.id) {
        <section hlmCard [attr.aria-labelledby]="'team-' + team.id">
          <div hlmCardHeader class="flex flex-wrap items-start justify-between gap-2">
            <div class="flex flex-col gap-1">
              <h2 hlmCardTitle [id]="'team-' + team.id">{{ team.name }}</h2>
              <p hlmCardDescription>
                @if (team.manager; as manager) {
                  Manager: {{ manager.displayName }}
                } @else {
                  <span hlmBadge variant="outline">
                    <ng-icon name="lucideUserX" aria-hidden="true" />
                    No manager
                  </span>
                }
              </p>
            </div>
            <hl-rename-team-action
              [team]="team"
              (changed)="changed(invites)"
              (failed)="error.set($event)"
            />
          </div>
          <div hlmCardContent>
            <hl-user-table
              [users]="team.members"
              [caption]="'Members of ' + team.name"
              emptyText="No members yet."
            >
              <ng-template hlUserRowActions let-user>
                <hl-user-activation-action
                  scope="users"
                  [user]="user"
                  (changed)="changed(invites)"
                  (failed)="error.set($event)"
                />
                @if (user.role === 'Interviewer') {
                  <hl-reassign-action
                    [user]="user"
                    [teams]="teamOptions()"
                    (changed)="changed(invites)"
                    (failed)="error.set($event)"
                  />
                }
              </ng-template>
            </hl-user-table>
          </div>
        </section>
      } @empty {
        @if (loaded()) {
          <p class="rounded-md border border-dashed p-3 text-center text-muted-foreground">
            No teams yet. A team is created when its first manager accepts an invite.
          </p>
        }
      }
    </div>

    <hl-invites-section #invites class="mt-6" kind="managers" />
  `,
})
export class TeamsPage implements OnInit {
  private readonly usersService = inject(UsersService);

  protected readonly teams = signal<TeamSummary[]>([]);
  protected readonly loaded = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly teamOptions = computed(() =>
    this.teams().map((team) => ({ id: team.id, name: team.name })),
  );

  ngOnInit(): void {
    void this.refresh();
  }

  /** After a change to a team or user: a deactivated manager's team can now be handed over. */
  protected async changed(invites: InvitesSection): Promise<void> {
    await Promise.all([this.refresh(), invites.refresh()]);
  }

  private async refresh(): Promise<void> {
    const result = await this.usersService.teams();
    this.loaded.set(true);
    if (result.ok) {
      this.error.set(null);
      this.teams.set(result.value);
    } else {
      this.error.set(result.message);
    }
  }
}
