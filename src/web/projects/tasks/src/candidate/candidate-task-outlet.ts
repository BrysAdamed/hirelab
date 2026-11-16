import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  inputBinding,
  model,
  output,
  outputBinding,
  untracked,
  viewChild,
  ViewContainerRef,
} from '@angular/core';
import { CANDIDATE_TASK_TYPES } from './candidate-task-types';

/**
 * Where a task type's `candidate` component is: `loading` its chunk, `ready`, `unavailable` (the app has no component
 * for the task type) or `failed` to load.
 */
export type CandidateTaskOutletState = 'loading' | 'ready' | 'unavailable' | 'failed';

/**
 * Renders the `candidate` component of `taskType` (from `CANDIDATE_TASK_TYPES`) with `content`, the candidate part of
 * a task version's content, and two-way binds `response`. The component is created again when the task type or the
 * content changes. The host shows the states it reports through `stateChange`.
 */
@Component({
  selector: 'hl-candidate-task-outlet',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<ng-container #outlet />`,
})
export class CandidateTaskOutlet {
  private readonly registry = inject(CANDIDATE_TASK_TYPES);
  private readonly outlet = viewChild.required('outlet', { read: ViewContainerRef });

  /** The task type key whose `candidate` component is rendered. */
  readonly taskType = input.required<string>();
  /** The candidate part of the content, as the candidate API sends it. */
  readonly content = input.required<unknown>();
  /** The candidate's response; `null` until they start. */
  readonly response = model<unknown>(null);
  readonly stateChange = output<CandidateTaskOutletState>();

  constructor() {
    effect((onCleanup) => {
      const taskType = this.taskType();
      this.content();
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
        outputBinding('responseChange', (response: unknown) => this.response.set(response)),
      ],
    });
    this.stateChange.emit('ready');
  }
}
