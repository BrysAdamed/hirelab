import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideLock } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmDialogImports } from '@hirelab/ui/helm/dialog';
import { PositionsService } from './positions.service';

/** Closes a job position, which anonymises its candidates, after confirmation. */
@Component({
  selector: 'hl-close-position-action',
  imports: [NgIcon, HlmButton, HlmDialogImports],
  providers: [provideIcons({ lucideLock })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <hlm-dialog>
      <button hlmDialogTrigger hlmBtn type="button" variant="outline" size="sm" [disabled]="pending()">
        <ng-icon name="lucideLock" aria-hidden="true" />
        Close position
      </button>
      <ng-template hlmDialogPortal>
        <hlm-dialog-content>
          <hlm-dialog-header>
            <h3 hlmDialogTitle>Close {{ position().title }}</h3>
            <div hlmDialogDescription class="flex flex-col gap-2">
              <p>
                Hiring ends and the position becomes read-only. Every candidate is anonymised: their
                name, email, phone and profile links are removed and their attachments are deleted.
                They are shown as “Candidate #N” from then on.
              </p>
              <p>Notes, scores and task results are kept. This can’t be undone.</p>
            </div>
          </hlm-dialog-header>
          <hlm-dialog-footer>
            <button hlmBtn hlmDialogClose type="button" variant="outline">Cancel</button>
            <button hlmBtn hlmDialogClose type="button" variant="destructive" (click)="close()">
              Close and anonymise
            </button>
          </hlm-dialog-footer>
        </hlm-dialog-content>
      </ng-template>
    </hlm-dialog>
  `,
})
export class ClosePositionAction {
  private readonly positionsService = inject(PositionsService);

  readonly position = input.required<{ id: string; title: string; version: number }>();
  /** Emitted after the position is closed. */
  readonly closed = output();
  /** Emitted with the message of a refused close. */
  readonly failed = output<string>();

  protected readonly pending = signal(false);

  protected async close(): Promise<void> {
    this.pending.set(true);
    const position = this.position();
    const result = await this.positionsService.close(position.id, position.version);
    this.pending.set(false);
    if (result.ok) {
      this.closed.emit();
    } else {
      this.failed.emit(result.message);
    }
  }
}
