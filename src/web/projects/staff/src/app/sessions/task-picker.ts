import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCheck, lucideCircleAlert, lucidePlus } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import type { TaskSummary } from '../api/staff-api';
import { noFilter, type TaskBankFilter } from '../task-bank/task-bank-filter';
import { TaskBankFilters } from '../task-bank/task-bank-filters';
import { TaskBankService } from '../task-bank/task-bank.service';
import { TaskFacts } from '../task-bank/task-facts';
import { TaskVisibilityBadge } from '../task-bank/task-visibility-badge';

/** The task bank list with its filters, for picking tasks to queue. Tasks already queued can't be picked again. */
@Component({
  selector: 'hl-task-picker',
  imports: [NgIcon, HlmButton, TaskBankFilters, TaskFacts, TaskVisibilityBadge],
  providers: [provideIcons({ lucideCheck, lucideCircleAlert, lucidePlus })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <hl-task-bank-filters
      [filter]="filter()"
      [tags]="tags()"
      [taskTypes]="taskTypes()"
      (filterChange)="filter.set($event)"
    />

    @if (error(); as message) {
      <div role="alert" class="mt-3 flex items-start gap-2 rounded-md border border-destructive p-2">
        <ng-icon
          name="lucideCircleAlert"
          size="16"
          class="mt-0.5 shrink-0 text-destructive"
          aria-hidden="true"
        />
        <p>{{ message }}</p>
      </div>
    } @else if (loaded()) {
      @if (tasks().length === 0) {
        <p class="mt-3 rounded-md border border-dashed p-3 text-center text-muted-foreground">
          No tasks match these filters.
        </p>
      } @else {
        <ul class="mt-3 flex flex-col gap-2" aria-label="Task bank">
          @for (task of tasks(); track task.id) {
            <li class="flex flex-wrap items-start justify-between gap-2 rounded-md border p-3">
              <div class="flex min-w-0 flex-1 flex-col gap-1">
                <div class="flex flex-wrap items-center gap-2">
                  <span class="font-medium">{{ task.title }}</span>
                  <hl-task-visibility-badge [visibility]="task.visibility" />
                </div>
                <p class="line-clamp-2 text-muted-foreground" [title]="task.summary">
                  {{ task.summary }}
                </p>
                <hl-task-facts [task]="task" />
              </div>
              @if (queuedTaskIds().includes(task.id)) {
                <span class="flex items-center gap-1 text-muted-foreground">
                  <ng-icon name="lucideCheck" aria-hidden="true" />
                  Queued
                </span>
              } @else {
                <button
                  hlmBtn
                  type="button"
                  size="sm"
                  variant="outline"
                  [attr.aria-label]="'Add ' + task.title + ' to the queue'"
                  (click)="picked.emit(task.id)"
                >
                  <ng-icon name="lucidePlus" aria-hidden="true" />
                  Add
                </button>
              }
            </li>
          }
        </ul>
      }
    }
  `,
})
export class TaskPicker {
  private readonly taskBank = inject(TaskBankService);
  private latestRequest = 0;

  /** The tasks already in the queue. */
  readonly queuedTaskIds = input<readonly string[]>([]);
  /** Emitted with the id of the task to add to the queue. */
  readonly picked = output<string>();

  protected readonly filter = signal<TaskBankFilter>(noFilter);
  protected readonly tasks = signal<TaskSummary[]>([]);
  protected readonly tags = signal<string[]>([]);
  protected readonly taskTypes = signal<string[]>([]);
  protected readonly loaded = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    effect(() => void this.load(this.filter()));
    void this.loadFilterOptions();
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
