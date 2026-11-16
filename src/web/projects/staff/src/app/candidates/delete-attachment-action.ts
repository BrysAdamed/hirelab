import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideTrash2 } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmDialogImports } from '@hirelab/ui/helm/dialog';

/** The delete button of an attachment, with a confirmation dialog. */
@Component({
  selector: 'hl-delete-attachment-action',
  imports: [NgIcon, HlmButton, HlmDialogImports],
  providers: [provideIcons({ lucideTrash2 })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <hlm-dialog>
      <button
        hlmDialogTrigger
        hlmBtn
        type="button"
        variant="ghost"
        size="sm"
        [disabled]="disabled()"
        [attr.aria-label]="'Delete ' + fileName()"
      >
        <ng-icon name="lucideTrash2" aria-hidden="true" />
      </button>
      <ng-template hlmDialogPortal>
        <hlm-dialog-content>
          <hlm-dialog-header>
            <h3 hlmDialogTitle>Delete {{ fileName() }}</h3>
            <p hlmDialogDescription>The file is deleted for good. This can’t be undone.</p>
          </hlm-dialog-header>
          <hlm-dialog-footer>
            <button hlmBtn hlmDialogClose type="button" variant="outline">Cancel</button>
            <button
              hlmBtn
              hlmDialogClose
              type="button"
              variant="destructive"
              (click)="confirmed.emit()"
            >
              Delete file
            </button>
          </hlm-dialog-footer>
        </hlm-dialog-content>
      </ng-template>
    </hlm-dialog>
  `,
})
export class DeleteAttachmentAction {
  readonly fileName = input.required<string>();
  readonly disabled = input(false);
  /** Emitted when the user confirms the delete. */
  readonly confirmed = output();
}
