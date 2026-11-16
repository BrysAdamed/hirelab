import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  inputBinding,
  output,
  untracked,
  viewChild,
  ViewContainerRef,
} from '@angular/core';
import type { TaskContent } from '@hirelab/tasks/framework';
import { REVIEW_TASK_TYPES } from './review-task-types';

/**
 * Where a task type's `review` component is: `loading` its chunk, `ready`, `unavailable` (the app has no component
 * for the task type) or `failed` to load.
 */
export type ReviewTaskOutletState = 'loading' | 'ready' | 'unavailable' | 'failed';

/**
 * Renders the `review` component of `taskType` (from `REVIEW_TASK_TYPES`), read-only, with the task's content, the
 * candidate's response, the reference answer and the interviewer notes. The component is created again only when the
 * task type changes; the other inputs flow into it. The host shows the states it reports through `stateChange`.
 */
@Component({
  selector: 'hl-review-task-outlet',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<ng-container #outlet />`,
})
export class ReviewTaskOutlet {
  private readonly registry = inject(REVIEW_TASK_TYPES);
  private readonly outlet = viewChild.required('outlet', { read: ViewContainerRef });

  /** The task type key whose `review` component is rendered. */
  readonly taskType = input.required<string>();
  readonly content = input.required<TaskContent<unknown, unknown>>();
  /** The candidate's response; `null` before they saved anything. */
  readonly response = input<unknown>(null);
  readonly referenceAnswer = input.required<string>();
  readonly interviewerNotes = input<string | undefined>();
  /** Whether the candidate may still be changing the response: the task is Current and the session Live. */
  readonly live = input(false);
  readonly stateChange = output<ReviewTaskOutletState>();

  constructor() {
    effect((onCleanup) => {
      const taskType = this.taskType();
      const outlet = this.outlet();
      let current = true;
      onCleanup(() => {
        current = false;
        outlet.clear();
      });
      untracked(() => void this.render(taskType, outlet, () => current));
    });
  }

  private async render(
    taskType: string,
    outlet: ViewContainerRef,
    isCurrent: () => boolean,
  ): Promise<void> {
    const entry = this.registry.find(taskType);
    if (!entry) {
      this.stateChange.emit('unavailable');
      return;
    }

    this.stateChange.emit('loading');
    let component;
    try {
      component = await entry.loadComponent();
    } catch {
      if (isCurrent()) {
        this.stateChange.emit('failed');
      }
      return;
    }

    if (!isCurrent()) {
      return;
    }
    outlet.createComponent(component, {
      bindings: [
        inputBinding('content', this.content),
        inputBinding('response', this.response),
        inputBinding('referenceAnswer', this.referenceAnswer),
        inputBinding('interviewerNotes', this.interviewerNotes),
        inputBinding('live', this.live),
      ],
    });
    this.stateChange.emit('ready');
  }
}
