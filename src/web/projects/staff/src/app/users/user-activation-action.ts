import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmDialogImports } from '@hirelab/ui/helm/dialog';
import type { UserSummary } from '../api/staff-api';
import { AuthService } from '../auth/auth.service';
import { type UserScope, UsersService } from './users.service';

/**
 * Deactivates a user after confirmation, or reactivates a deactivated one. Shows nothing on the signed-in user's own
 * row: nobody deactivates themselves.
 */
@Component({
  selector: 'hl-user-activation-action',
  imports: [HlmButton, HlmDialogImports],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!isSelf()) {
      @if (user().isActive) {
        <hlm-dialog>
          <button
            hlmDialogTrigger
            hlmBtn
            type="button"
            variant="outline"
            size="sm"
            [disabled]="pending()"
            [attr.aria-label]="'Deactivate ' + user().displayName"
          >
            Deactivate
          </button>
          <ng-template hlmDialogPortal>
            <hlm-dialog-content>
              <hlm-dialog-header>
                <h3 hlmDialogTitle>Deactivate {{ user().displayName }}</h3>
                <p hlmDialogDescription>{{ consequences() }}</p>
              </hlm-dialog-header>
              <hlm-dialog-footer>
                <button hlmBtn hlmDialogClose type="button" variant="outline">Cancel</button>
                <button
                  hlmBtn
                  hlmDialogClose
                  type="button"
                  variant="destructive"
                  (click)="deactivate()"
                >
                  Deactivate user
                </button>
              </hlm-dialog-footer>
            </hlm-dialog-content>
          </ng-template>
        </hlm-dialog>
      } @else {
        <button
          hlmBtn
          type="button"
          variant="outline"
          size="sm"
          [disabled]="pending()"
          [attr.aria-label]="'Reactivate ' + user().displayName"
          (click)="reactivate()"
        >
          Reactivate
        </button>
      }
    }
  `,
})
export class UserActivationAction {
  private readonly usersService = inject(UsersService);
  private readonly auth = inject(AuthService);

  readonly user = input.required<UserSummary>();
  readonly scope = input.required<UserScope>();
  /** Emitted after a successful change, so the page reloads its list. */
  readonly changed = output();
  /** Emitted with the message of a refused change. */
  readonly failed = output<string>();

  protected readonly pending = signal(false);
  protected readonly isSelf = computed(() => this.auth.user()?.id === this.user().id);
  protected readonly consequences = computed(() =>
    this.user().role === 'Manager'
      ? 'They can’t log in any more and are logged out within 5 minutes. Their pending invites are revoked, and the team shows “No manager”. You can reactivate them later.'
      : 'They can’t log in any more and are logged out within 5 minutes. You can reactivate them later.',
  );

  protected deactivate(): Promise<void> {
    return this.run((userId) => this.usersService.deactivate(this.scope(), userId));
  }

  protected reactivate(): Promise<void> {
    return this.run((userId) => this.usersService.reactivate(this.scope(), userId));
  }

  private async run(
    change: (userId: string) => Promise<{ ok: true } | { ok: false; message: string }>,
  ): Promise<void> {
    this.pending.set(true);
    const result = await change(this.user().id);
    this.pending.set(false);
    if (result.ok) {
      this.changed.emit();
    } else {
      this.failed.emit(result.message);
    }
  }
}
