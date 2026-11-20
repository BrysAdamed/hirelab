import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MarkdownViewer } from '@hirelab/ui/markdown';
import type { ReviewComment } from './code-review';
import { lineRangeLabel } from './review-threads';

/** A review comment, read-only: the lines it is on and its Markdown body. Projected content (actions) goes on the right. */
@Component({
  selector: 'hl-review-comment',
  imports: [MarkdownViewer],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-col gap-1', role: 'article', '[attr.aria-label]': 'label()' },
  template: `
    <div class="flex items-center justify-between gap-2">
      <span class="font-mono text-xs text-muted-foreground">{{ lines() }}</span>
      <div class="flex items-center gap-1">
        <ng-content />
      </div>
    </div>
    <hl-markdown [markdown]="comment().body" />
  `,
})
export class ReviewCommentView {
  readonly comment = input.required<ReviewComment>();

  protected readonly lines = computed(() => {
    const label = lineRangeLabel(this.comment());
    return label.charAt(0).toUpperCase() + label.slice(1);
  });
  protected readonly label = computed(() => `Comment on ${lineRangeLabel(this.comment())}`);
}
