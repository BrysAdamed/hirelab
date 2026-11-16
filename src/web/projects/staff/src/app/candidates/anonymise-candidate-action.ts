import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideUserX } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmDialogImports } from '@hirelab/ui/helm/dialog';
import { CandidatesService } from './candidates.service';

/** Anonymises one candidate after confirmation. */
@Component({
  selector: 'hl-anonymise-candidate-action',
  imports: [NgIcon, HlmButton, HlmDialogImports],
  providers: [provideIcons({ lucideUserX })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <hlm-dialog>
      <button hlmDialogTrigger hlmBtn type="button" variant="outline" size="sm" [disabled]="pending()">
        <ng-icon name="lucideUserX" aria-hidden="true" />
        Anonymise
      </button>
      <ng-template hlmDialogPortal>
        <hlm-dialog-content>
          <hlm-dialog-header>
            <h3 hlmDialogTitle>Anonymise {{ candidate().displayName }}</h3>
            <div hlmDialogDescription class="flex flex-col gap-2">
              <p>
                Their name, email, phone and profile links are removed and their attachments are
                deleted. They are shown as “Candidate #{{ candidate().number }}” from then on.
              </p>
              <p>Notes, scores and task results are kept. This can’t be undone.</p>
            </div>
          </hlm-dialog-header>
          <hlm-dialog-footer>
            <button hlmBtn hlmDialogClose type="button" variant="outline">Cancel</button>
            <button
              hlmBtn
              hlmDialogClose
              type="button"
              variant="destructive"
              (click)="anonymise()"
            >
              Anonymise candidate
            </button>
          </hlm-dialog-footer>
        </hlm-dialog-content>
      </ng-template>
    </hlm-dialog>
  `,
})
export class AnonymiseCandidateAction {
  private readonly candidatesService = inject(CandidatesService);

  readonly candidate = input.required<{
    id: string;
    displayName: string;
    number: number;
    version: number;
  }>();
  /** Emitted after the candidate is anonymised. */
  readonly anonymised = output();
  /** Emitted with the message of a refused anonymisation. */
  readonly failed = output<string>();

  protected readonly pending = signal(false);

  protected async anonymise(): Promise<void> {
    this.pending.set(true);
    const candidate = this.candidate();
    const result = await this.candidatesService.anonymise(candidate.id, candidate.version);
    this.pending.set(false);
    if (result.ok) {
      this.anonymised.emit();
    } else {
      this.failed.emit(result.message);
    }
  }
}
