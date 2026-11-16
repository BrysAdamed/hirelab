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
import { HlmInput } from '@hirelab/ui/helm/input';
import { HlmLabel } from '@hirelab/ui/helm/label';
import type { UserSummary } from '../api/staff-api';
import { UsersService } from './users.service';

/** A team an interviewer can move to. */
export interface TeamOption {
  id: string;
  name: string;
}

/** Moves an interviewer to another team, chosen in a dialog. */
@Component({
  selector: 'hl-reassign-action',
  imports: [HlmButton, HlmDialogImports, HlmInput, HlmLabel],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (otherTeams().length > 0) {
      <hlm-dialog (stateChanged)="$event === 'open' && selectedTeamId.set(otherTeams()[0].id)">
        <button
          hlmDialogTrigger
          hlmBtn
          type="button"
          variant="outline"
          size="sm"
          [disabled]="pending()"
          [attr.aria-label]="'Reassign ' + user().displayName"
        >
          Reassign
        </button>
        <ng-template hlmDialogPortal>
          <hlm-dialog-content>
            <hlm-dialog-header>
              <h3 hlmDialogTitle>Reassign {{ user().displayName }}</h3>
              <p hlmDialogDescription>
                They are logged out within 5 minutes and from then on see only the new team’s work.
              </p>
            </hlm-dialog-header>
            <div class="flex flex-col gap-1.5">
              <label hlmLabel [for]="selectId()">New team</label>
              <select
                hlmInput
                [id]="selectId()"
                [value]="selectedTeamId()"
                (change)="selectedTeamId.set($any($event.target).value)"
              >
                @for (team of otherTeams(); track team.id) {
                  <option [value]="team.id">{{ team.name }}</option>
                }
              </select>
            </div>
            <hlm-dialog-footer>
              <button hlmBtn hlmDialogClose type="button" variant="outline">Cancel</button>
              <button hlmBtn hlmDialogClose type="button" (click)="reassign()">Move to team</button>
            </hlm-dialog-footer>
          </hlm-dialog-content>
        </ng-template>
      </hlm-dialog>
    }
  `,
})
export class ReassignAction {
  private readonly usersService = inject(UsersService);

  readonly user = input.required<UserSummary>();
  readonly teams = input.required<readonly TeamOption[]>();
  /** Emitted after a successful move, so the page reloads its list. */
  readonly changed = output();
  /** Emitted with the message of a refused move. */
  readonly failed = output<string>();

  protected readonly pending = signal(false);
  protected readonly selectedTeamId = signal('');
  protected readonly selectId = computed(() => `reassign-${this.user().id}`);
  protected readonly otherTeams = computed(() =>
    this.teams().filter((team) => team.id !== this.user().team?.id),
  );

  protected async reassign(): Promise<void> {
    const teamId = this.selectedTeamId() || this.otherTeams()[0]?.id;
    if (!teamId) {
      return;
    }
    this.pending.set(true);
    const result = await this.usersService.reassign(this.user().id, teamId);
    this.pending.set(false);
    if (result.ok) {
      this.changed.emit();
    } else {
      this.failed.emit(result.message);
    }
  }
}
