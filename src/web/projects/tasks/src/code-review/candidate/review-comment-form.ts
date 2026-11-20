import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  linkedSignal,
  output,
} from '@angular/core';
import type { DiffLineRange } from '@hirelab/ui/code';
import { HlmButton } from '@hirelab/ui/helm/button';
import { codeReviewLimits } from './code-review';
import { MarkdownField } from './markdown-field';
import { lineRangeLabel } from './review-threads';

/** Writes a new review comment or edits one. `Ctrl+Enter` saves and `Escape` cancels. */
@Component({
  selector: 'hl-review-comment-form',
  imports: [HlmButton, MarkdownField],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'flex flex-col gap-2',
    '(keydown.escape)': 'onEscape($event)',
    '(keydown.control.enter)': 'submit()',
    '(keydown.meta.enter)': 'submit()',
  },
  template: `
    <hl-markdown-field
      [label]="label()"
      [value]="body()"
      [maxLength]="maxLength"
      [focusOnInit]="true"
      (valueChange)="body.set($event)"
    />
    <div class="flex items-center justify-end gap-2">
      <button hlmBtn type="button" variant="ghost" size="sm" (click)="cancelled.emit()">
        Cancel
      </button>
      <button hlmBtn type="button" size="sm" [disabled]="!canSave()" (click)="submit()">
        {{ submitLabel() }}
      </button>
    </div>
  `,
})
export class ReviewCommentForm {
  readonly range = input.required<DiffLineRange>();
  /** The body to start from: empty for a new comment, the saved body when editing. */
  readonly initialBody = input('');
  readonly submitLabel = input('Add comment');
  readonly saved = output<string>();
  readonly cancelled = output();

  protected readonly maxLength = codeReviewLimits.bodyMaxLength;
  protected readonly body = linkedSignal(() => this.initialBody());
  protected readonly label = computed(() => `Comment on ${lineRangeLabel(this.range())}`);
  protected readonly canSave = computed(() => {
    const body = this.body();
    return body.trim().length > 0 && body.length <= this.maxLength;
  });

  protected submit(): void {
    if (this.canSave()) {
      this.saved.emit(this.body());
    }
  }

  protected onEscape(event: Event): void {
    event.preventDefault();
    this.cancelled.emit();
  }
}
