import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MarkdownViewer } from '@hirelab/ui/markdown';

/** The pull request's title and its Markdown description. */
@Component({
  selector: 'hl-pull-request-header',
  imports: [MarkdownViewer],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-col gap-2' },
  template: `
    <h2 class="text-xl font-semibold">{{ title() }}</h2>
    @if (description().trim()) {
      <hl-markdown [markdown]="description()" />
    }
  `,
})
export class PullRequestHeader {
  readonly title = input.required<string>();
  readonly description = input('');
}
