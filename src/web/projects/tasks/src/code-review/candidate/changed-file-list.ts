import { ChangeDetectionStrategy, Component, computed, input, model } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideFileMinus,
  lucideFilePen,
  lucideFilePlus,
  lucideMessageSquare,
} from '@ng-icons/lucide';
import { HlmBadge } from '@hirelab/ui/helm/badge';
import type { ChangedFile, ReviewComment } from './code-review';
import { changedFileStatus, commentCounts, type ChangedFileStatus } from './review-threads';

const statusIcons: Record<ChangedFileStatus, string> = {
  Added: 'lucideFilePlus',
  Modified: 'lucideFilePen',
  Deleted: 'lucideFileMinus',
};

/** The pull request's changed files with their status and comment count; picking one selects it. */
@Component({
  selector: 'hl-changed-file-list',
  imports: [NgIcon, HlmBadge],
  providers: [
    provideIcons({ lucideFileMinus, lucideFilePen, lucideFilePlus, lucideMessageSquare }),
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex min-w-0 flex-col gap-2' },
  template: `
    <h3 class="font-semibold">Files changed ({{ files().length }})</h3>
    <nav aria-label="Changed files">
      <ul class="flex flex-col gap-1">
        @for (item of items(); track item.file.path) {
          <li>
            <button
              type="button"
              class="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left hover:bg-muted focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none aria-[current=true]:bg-accent aria-[current=true]:text-accent-foreground aria-[current=true]:font-medium"
              [attr.aria-current]="item.file.path === selected() ? 'true' : null"
              (click)="selected.set(item.file.path)"
            >
              <span class="min-w-0 flex-1 truncate font-mono text-sm" [title]="item.file.path">
                {{ item.file.path }}
              </span>
              <span hlmBadge variant="outline">
                <ng-icon [name]="item.icon" aria-hidden="true" />
                {{ item.status }}
              </span>
              <span class="flex items-center gap-1 text-sm text-muted-foreground">
                <ng-icon name="lucideMessageSquare" aria-hidden="true" />
                {{ item.comments }}
                <span class="sr-only">{{ item.comments === 1 ? 'comment' : 'comments' }}</span>
              </span>
            </button>
          </li>
        }
      </ul>
    </nav>
  `,
})
export class ChangedFileList {
  readonly files = input.required<readonly ChangedFile[]>();
  readonly comments = input<readonly ReviewComment[]>([]);
  /** The path of the selected file. */
  readonly selected = model.required<string>();

  protected readonly items = computed(() => {
    const counts = commentCounts(this.comments());
    return this.files().map((file) => {
      const status = changedFileStatus(file);
      return { file, status, icon: statusIcons[status], comments: counts.get(file.path) ?? 0 };
    });
  });
}
