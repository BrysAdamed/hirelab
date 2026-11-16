import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideChevronDown, lucideKeyRound, lucideLogOut, lucideUserRound } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmDropdownMenuImports } from '@hirelab/ui/helm/dropdown-menu';
import { AuthService } from '../auth/auth.service';

/** The signed-in user's name in the header, opening a menu with their details and account actions. */
@Component({
  selector: 'hl-user-menu',
  imports: [NgIcon, HlmButton, HlmDropdownMenuImports],
  providers: [provideIcons({ lucideChevronDown, lucideKeyRound, lucideLogOut, lucideUserRound })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (auth.user(); as user) {
      <button
        hlmBtn
        type="button"
        variant="ghost"
        size="sm"
        [hlmDropdownMenuTrigger]="menu"
        align="end"
      >
        <ng-icon name="lucideUserRound" aria-hidden="true" />
        <span>{{ user.displayName }}</span>
        <ng-icon name="lucideChevronDown" aria-hidden="true" />
      </button>
      <ng-template #menu>
        <hlm-dropdown-menu class="w-64">
          <div hlmDropdownMenuLabel class="flex flex-col gap-0.5">
            <span class="text-sm text-popover-foreground">{{ user.displayName }}</span>
            <span class="truncate">{{ user.email }}</span>
            <span
              >{{ user.role }}
              @if (user.team; as team) {
                · {{ team.name }}
              }
            </span>
          </div>
          <hlm-dropdown-menu-separator />
          <button hlmDropdownMenuItem type="button" (triggered)="changePassword()">
            <ng-icon name="lucideKeyRound" aria-hidden="true" />
            Change password
          </button>
          <button hlmDropdownMenuItem type="button" (triggered)="auth.logout()">
            <ng-icon name="lucideLogOut" aria-hidden="true" />
            Log out
          </button>
        </hlm-dropdown-menu>
      </ng-template>
    }
  `,
})
export class UserMenu {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected changePassword(): void {
    void this.router.navigateByUrl('/account/password');
  }
}
