import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert } from '@ng-icons/lucide';
import type { UserSummary } from '../api/staff-api';
import { UserActivationAction } from '../users/user-activation-action';
import { UserRowActions, UserTable } from '../users/user-table';
import { UsersService } from '../users/users.service';

/** The admin's Users page: every staff user, active and deactivated. */
@Component({
  selector: 'hl-users-page',
  imports: [NgIcon, UserActivationAction, UserRowActions, UserTable],
  providers: [provideIcons({ lucideCircleAlert })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="text-xl font-semibold">Users</h1>
    <p class="mt-1 text-muted-foreground">
      Deactivated users can't log in. Nobody is deleted, so their work keeps its author.
    </p>

    <div class="mt-3 flex flex-col gap-2">
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
        <hl-user-table caption="Users" [users]="users()" [showTeam]="true">
          <ng-template hlUserRowActions let-user>
            <hl-user-activation-action
              scope="users"
              [user]="user"
              (changed)="refresh()"
              (failed)="error.set($event)"
            />
          </ng-template>
        </hl-user-table>
      }
    </div>
  `,
})
export class UsersPage implements OnInit {
  private readonly usersService = inject(UsersService);

  protected readonly users = signal<UserSummary[]>([]);
  protected readonly loaded = signal(false);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    void this.refresh();
  }

  protected async refresh(): Promise<void> {
    const result = await this.usersService.list('users');
    this.loaded.set(true);
    if (result.ok) {
      this.error.set(null);
      this.users.set(result.value);
    } else {
      this.error.set(result.message);
    }
  }
}
