import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import type { TaskSummary } from '../api/staff-api';

/** A task's task type, level, estimated minutes and tags. */
@Component({
  selector: 'hl-task-facts',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <dl class="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm">
      <div class="flex gap-1">
        <dt class="text-muted-foreground">Type</dt>
        <dd class="font-mono">{{ task().taskType }}</dd>
      </div>
      <div class="flex gap-1">
        <dt class="text-muted-foreground">Level</dt>
        <dd>{{ task().level }}</dd>
      </div>
      <div class="flex gap-1">
        <dt class="text-muted-foreground">Estimate</dt>
        <dd>{{ task().estimatedMinutes }} min</dd>
      </div>
      @if (task().tags.length > 0) {
        <div class="flex flex-wrap items-center gap-1">
          <dt class="text-muted-foreground">Tags</dt>
          @for (tag of task().tags; track tag) {
            <dd class="rounded-sm bg-muted px-1.5 font-mono text-xs">{{ tag }}</dd>
          }
        </div>
      }
    </dl>
  `,
})
export class TaskFacts {
  readonly task = input.required<TaskSummary>();
}
