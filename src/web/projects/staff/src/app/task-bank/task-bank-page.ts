import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert } from '@ng-icons/lucide';
import type { TaskSummary } from '../api/staff-api';
import {
  filterFromQuery,
  isFiltered,
  queryFromFilter,
  type TaskBankFilter,
} from './task-bank-filter';
import { TaskBankFilters } from './task-bank-filters';
import { TaskBankService } from './task-bank.service';
import { TaskFacts } from './task-facts';
import { TaskVisibilityBadge } from './task-visibility-badge';

/** The task bank: the active version of every task the user can see, filtered by the query parameters. */
@Component({
  selector: 'hl-task-bank-page',
  imports: [RouterLink, NgIcon, TaskBankFilters, TaskFacts, TaskVisibilityBadge],
  providers: [provideIcons({ lucideCircleAlert })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="text-xl font-semibold">Task bank</h1>

    <hl-task-bank-filters
      class="mt-3 block"
      [filter]="filter()"
      [tags]="tags()"
      [taskTypes]="taskTypes()"
      (filterChange)="applyFilter($event)"
    />

    @if (error(); as message) {
      <div
        role="alert"
        class="mt-3 flex items-start gap-2 rounded-md border border-destructive p-2"
      >
        <ng-icon
          name="lucideCircleAlert"
          size="16"
          class="mt-0.5 shrink-0 text-destructive"
          aria-hidden="true"
        />
        <p>{{ message }}</p>
      </div>
    }

    <p class="mt-3 text-muted-foreground" aria-live="polite">
      @if (loaded() && !error()) {
        {{ tasks().length === 1 ? '1 task' : tasks().length + ' tasks' }}
      }
    </p>

    @if (loaded() && !error()) {
      @if (tasks().length === 0) {
        <p class="mt-2 rounded-md border border-dashed p-3 text-center text-muted-foreground">
          {{ filtered() ? 'No tasks match these filters.' : 'The task bank is empty.' }}
        </p>
      } @else {
        <ul class="mt-2 flex flex-col gap-2" aria-label="Tasks">
          @for (task of tasks(); track task.id) {
            <li class="flex flex-col gap-1 rounded-md border p-3">
              <div class="flex flex-wrap items-center justify-between gap-2">
                <a
                  class="rounded-sm font-medium text-accent-text underline-offset-4 hover:underline focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
                  [routerLink]="['/tasks', task.id]"
                >
                  {{ task.title }}
                </a>
                <hl-task-visibility-badge [visibility]="task.visibility" />
              </div>
              <p class="line-clamp-2 text-muted-foreground" [title]="task.summary">
                {{ task.summary }}
              </p>
              <hl-task-facts [task]="task" />
            </li>
          }
        </ul>
      }
    }
  `,
})
export class TaskBankPage {
  private readonly taskBank = inject(TaskBankService);
  private readonly router = inject(Router);
  private latestRequest = 0;

  /** The `tag` query parameters: the task has every one of them. */
  readonly tag = input<string | readonly string[]>();
  /** The `level` query parameters: the task has one of them. */
  readonly level = input<string | readonly string[]>();
  /** The `type` query parameter: a task type key. */
  readonly type = input<string>();
  /** The `search` query parameter: text in the title or summary. */
  readonly search = input<string>();

  protected readonly filter = computed<TaskBankFilter>(
    () =>
      filterFromQuery({
        tag: this.tag(),
        level: this.level(),
        type: this.type(),
        search: this.search(),
      }),
    { equal: (left, right) => JSON.stringify(left) === JSON.stringify(right) },
  );
  protected readonly filtered = computed(() => isFiltered(this.filter()));
  protected readonly tasks = signal<TaskSummary[]>([]);
  protected readonly tags = signal<string[]>([]);
  protected readonly taskTypes = signal<string[]>([]);
  protected readonly loaded = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    effect(() => void this.load(this.filter()));
    void this.loadFilterOptions();
  }

  protected async applyFilter(filter: TaskBankFilter): Promise<void> {
    await this.router.navigate(['/tasks'], { queryParams: queryFromFilter(filter) });
  }

  private async load(filter: TaskBankFilter): Promise<void> {
    const request = ++this.latestRequest;
    const result = await this.taskBank.list(filter);
    if (request !== this.latestRequest) {
      return;
    }
    this.loaded.set(true);
    if (result.ok) {
      this.error.set(null);
      this.tasks.set(result.value);
    } else {
      this.error.set(result.message);
      this.tasks.set([]);
    }
  }

  private async loadFilterOptions(): Promise<void> {
    const [tags, taskTypes] = await Promise.all([this.taskBank.tags(), this.taskBank.taskTypes()]);
    if (tags.ok) {
      this.tags.set(tags.value);
    }
    if (taskTypes.ok) {
      this.taskTypes.set(taskTypes.value);
    }
  }
}
