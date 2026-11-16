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
import { UsersService } from '../users/users.service';

/** Renames a team in a dialog. */
@Component({
  selector: 'hl-rename-team-action',
  imports: [HlmButton, HlmDialogImports, HlmInput, HlmLabel],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <hlm-dialog #dialog="hlmDialog" (stateChanged)="$event === 'open' && name.set(team().name)">
      <button
        hlmDialogTrigger
        hlmBtn
        type="button"
        variant="outline"
        size="sm"
        [attr.aria-label]="'Rename ' + team().name"
      >
        Rename
      </button>
      <ng-template hlmDialogPortal>
        <hlm-dialog-content>
          <form class="flex flex-col gap-3" (submit)="$event.preventDefault(); rename(dialog)">
            <hlm-dialog-header>
              <h3 hlmDialogTitle>Rename {{ team().name }}</h3>
              <p hlmDialogDescription>The team keeps its members and all its work.</p>
            </hlm-dialog-header>
            <div class="flex flex-col gap-1.5">
              <label hlmLabel [for]="inputId()">Team name</label>
              <input
                hlmInput
                autocomplete="off"
                maxlength="100"
                [id]="inputId()"
                [value]="name()"
                (input)="name.set($any($event.target).value)"
              />
            </div>
            <hlm-dialog-footer>
              <button hlmBtn hlmDialogClose type="button" variant="outline">Cancel</button>
              <button hlmBtn type="submit" [disabled]="!name().trim() || pending()">
                Save name
              </button>
            </hlm-dialog-footer>
          </form>
        </hlm-dialog-content>
      </ng-template>
    </hlm-dialog>
  `,
})
export class RenameTeamAction {
  private readonly usersService = inject(UsersService);

  readonly team = input.required<{ id: string; name: string }>();
  /** Emitted after a successful rename, so the page reloads its list. */
  readonly changed = output();
  /** Emitted with the message of a refused rename. */
  readonly failed = output<string>();

  protected readonly name = signal('');
  protected readonly pending = signal(false);
  protected readonly inputId = computed(() => `rename-team-${this.team().id}`);

  protected async rename(dialog: { close: () => void }): Promise<void> {
    const name = this.name().trim();
    if (!name || this.pending()) {
      return;
    }
    this.pending.set(true);
    const result = await this.usersService.renameTeam(this.team().id, name);
    this.pending.set(false);
    dialog.close();
    if (result.ok) {
      this.changed.emit();
    } else {
      this.failed.emit(result.message);
    }
  }
}
