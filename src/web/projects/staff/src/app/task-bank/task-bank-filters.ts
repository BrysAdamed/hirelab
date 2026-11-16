import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  linkedSignal,
  output,
} from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideSearch, lucideX } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmInput } from '@hirelab/ui/helm/input';
import { HlmLabel } from '@hirelab/ui/helm/label';
import type { TaskLevel } from '../api/staff-api';
import { isFiltered, noFilter, type TaskBankFilter, taskLevels } from './task-bank-filter';

/** The task bank's filters. Every change emits the whole new filter; the search applies when submitted. */
@Component({
  selector: 'hl-task-bank-filters',
  imports: [NgIcon, HlmButton, HlmInput, HlmLabel],
  providers: [provideIcons({ lucideSearch, lucideX })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col gap-3 rounded-md border p-3">
      <div class="flex flex-wrap items-end gap-3">
        <form
          role="search"
          class="flex min-w-64 grow items-end gap-2"
          (submit)="submitSearch($event)"
        >
          <div class="flex grow flex-col gap-1">
            <label hlmLabel for="task-bank-search">Search</label>
            <input
              hlmInput
              id="task-bank-search"
              type="search"
              placeholder="Title or summary"
              [value]="search()"
              (input)="search.set($any($event.target).value)"
            />
          </div>
          <button hlmBtn type="submit" variant="secondary">
            <ng-icon name="lucideSearch" aria-hidden="true" />
            Search
          </button>
        </form>

        <div class="flex flex-col gap-1">
          <label hlmLabel for="task-bank-type">Task type</label>
          <select
            id="task-bank-type"
            class="h-9 rounded-md border border-input bg-background px-2.5 text-foreground outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
            (change)="chooseTaskType($any($event.target).value)"
          >
            <option value="" [selected]="filter().taskType === null">All task types</option>
            @for (taskType of taskTypeOptions(); track taskType) {
              <option [value]="taskType" [selected]="filter().taskType === taskType">
                {{ taskType }}
              </option>
            }
          </select>
        </div>
      </div>

      <div
        role="group"
        aria-labelledby="task-bank-levels"
        class="flex flex-wrap items-center gap-1"
      >
        <span id="task-bank-levels" class="mr-1 text-muted-foreground">Level</span>
        @for (level of levels; track level) {
          <button
            hlmBtn
            type="button"
            size="sm"
            [variant]="filter().levels.includes(level) ? 'secondary' : 'outline'"
            [attr.aria-pressed]="filter().levels.includes(level)"
            (click)="toggleLevel(level)"
          >
            {{ level }}
          </button>
        }
      </div>

      @if (tagOptions().length > 0) {
        <div
          role="group"
          aria-labelledby="task-bank-tags"
          class="flex flex-wrap items-center gap-1"
        >
          <span id="task-bank-tags" class="mr-1 text-muted-foreground">Tags</span>
          @for (tag of tagOptions(); track tag) {
            <button
              hlmBtn
              type="button"
              size="sm"
              class="font-mono"
              [variant]="filter().tags.includes(tag) ? 'secondary' : 'outline'"
              [attr.aria-pressed]="filter().tags.includes(tag)"
              (click)="toggleTag(tag)"
            >
              {{ tag }}
            </button>
          }
        </div>
      }

      @if (filtered()) {
        <div>
          <button
            hlmBtn
            type="button"
            variant="ghost"
            size="sm"
            (click)="filterChange.emit(noFilter)"
          >
            <ng-icon name="lucideX" aria-hidden="true" />
            Clear filters
          </button>
        </div>
      }
    </div>
  `,
})
export class TaskBankFilters {
  readonly filter = input.required<TaskBankFilter>();
  /** The tags to offer: the tags of the tasks in the bank. */
  readonly tags = input<readonly string[]>([]);
  /** The task types to offer: those of the tasks in the bank. */
  readonly taskTypes = input<readonly string[]>([]);
  readonly filterChange = output<TaskBankFilter>();

  protected readonly levels = taskLevels;
  protected readonly noFilter = noFilter;
  protected readonly search = linkedSignal(() => this.filter().search);
  protected readonly filtered = computed(() => isFiltered(this.filter()));
  protected readonly tagOptions = computed(() => withSelected(this.tags(), this.filter().tags));
  protected readonly taskTypeOptions = computed(() => {
    const taskType = this.filter().taskType;
    return withSelected(this.taskTypes(), taskType === null ? [] : [taskType]);
  });

  protected submitSearch(event: Event): void {
    event.preventDefault();
    this.change({ search: this.search().trim() });
  }

  protected chooseTaskType(taskType: string): void {
    this.change({ taskType: taskType || null });
  }

  protected toggleLevel(level: TaskLevel): void {
    const levels = this.filter().levels;
    this.change({
      levels: levels.includes(level)
        ? levels.filter((selected) => selected !== level)
        : this.levels.filter((known) => known === level || levels.includes(known)),
    });
  }

  protected toggleTag(tag: string): void {
    const tags = this.filter().tags;
    this.change({
      tags: tags.includes(tag) ? tags.filter((selected) => selected !== tag) : [...tags, tag],
    });
  }

  private change(change: Partial<TaskBankFilter>): void {
    this.filterChange.emit({ ...this.filter(), ...change });
  }
}

function withSelected(options: readonly string[], selected: readonly string[]): readonly string[] {
  const missing = selected.filter((value) => !options.includes(value));
  return missing.length === 0 ? options : [...options, ...missing];
}
