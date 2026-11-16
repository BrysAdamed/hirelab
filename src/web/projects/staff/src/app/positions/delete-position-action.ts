import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideTrash2 } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmDialogImports } from '@hirelab/ui/helm/dialog';
import { PositionsService } from './positions.service';

/** Deletes a job position after confirmation. */
@Component({
  selector: 'hl-delete-position-action',
  imports: [NgIcon, HlmButton, HlmDialogImports],
  providers: [provideIcons({ lucideTrash2 })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <hlm-dialog>
      <button hlmDialogTrigger hlmBtn type="button" variant="outline" size="sm" [disabled]="pending()">
        <ng-icon name="lucideTrash2" aria-hidden="true" />
        Delete
      </button>
      <ng-template hlmDialogPortal>
        <hlm-dialog-content>
          <hlm-dialog-header>
            <h3 hlmDialogTitle>Delete {{ position().title }}</h3>
            <p hlmDialogDescription>
              The position and everything written on it are deleted for good. This can’t be undone.
            </p>
          </hlm-dialog-header>
          <hlm-dialog-footer>
            <button hlmBtn hlmDialogClose type="button" variant="outline">Cancel</button>
            <button hlmBtn hlmDialogClose type="button" variant="destructive" (click)="delete()">
              Delete position
            </button>
          </hlm-dialog-footer>
        </hlm-dialog-content>
      </ng-template>
    </hlm-dialog>
  `,
})
export class DeletePositionAction {
  private readonly positionsService = inject(PositionsService);

  readonly position = input.required<{ id: string; title: string }>();
  /** Emitted after the position is deleted. */
  readonly deleted = output();
  /** Emitted with the message of a refused delete. */
  readonly failed = output<string>();

  protected readonly pending = signal(false);

  protected async delete(): Promise<void> {
    this.pending.set(true);
    const result = await this.positionsService.delete(this.position().id);
    this.pending.set(false);
    if (result.ok) {
      this.deleted.emit();
    } else {
      this.failed.emit(result.message);
    }
  }
}
