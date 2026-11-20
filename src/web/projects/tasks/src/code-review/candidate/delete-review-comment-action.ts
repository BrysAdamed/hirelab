import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideTrash2 } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmDialogImports } from '@hirelab/ui/helm/dialog';
import type { ReviewComment } from './code-review';
import { lineRangeLabel } from './review-threads';

/** The delete button of a review comment, with a confirmation dialog. */
@Component({
  selector: 'hl-delete-review-comment-action',
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
        size="xs"
        [attr.aria-label]="'Delete comment on ' + lines()"
      >
        <ng-icon name="lucideTrash2" aria-hidden="true" />
        Delete
      </button>
      <ng-template hlmDialogPortal>
        <hlm-dialog-content>
          <hlm-dialog-header>
            <h3 hlmDialogTitle>Delete comment</h3>
            <p hlmDialogDescription>
              Your comment on {{ lines() }} is deleted for good. This can’t be undone.
            </p>
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
              Delete comment
            </button>
          </hlm-dialog-footer>
        </hlm-dialog-content>
      </ng-template>
    </hlm-dialog>
  `,
})
export class DeleteReviewCommentAction {
  readonly comment = input.required<ReviewComment>();
  /** Emitted when the user confirms the delete. */
  readonly confirmed = output();

  protected readonly lines = computed(() => lineRangeLabel(this.comment()));
}
