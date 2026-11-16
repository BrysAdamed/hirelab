import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  inputBinding,
  outputBinding,
  signal,
  untracked,
  viewChild,
  ViewContainerRef,
} from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert, lucideEyeOff, lucideRotateCcw } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { CANDIDATE_TASK_TYPES } from '@hirelab/tasks/candidate';

type PreviewState = 'loading' | 'ready' | 'unavailable' | 'failed';

/**
 * Shows staff what the candidate sees: renders the `candidate` component of `taskType` (from `CANDIDATE_TASK_TYPES`)
 * with `content`, the candidate part of a task version's content. The response starts empty (`null`) and lives only
 * in this component, so nothing is saved; "Reset" starts over. A task type without a registered component shows a
 * "no preview" state.
 */
@Component({
  selector: 'hl-task-preview',
  imports: [NgIcon, HlmButton],
  providers: [provideIcons({ lucideCircleAlert, lucideEyeOff, lucideRotateCcw })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-wrap items-center justify-between gap-2">
      <p class="text-muted-foreground">Answers you enter here are not saved.</p>
      @if (state() === 'ready') {
        <button hlmBtn type="button" variant="outline" size="sm" (click)="reset()">
          <ng-icon name="lucideRotateCcw" aria-hidden="true" />
          Reset preview
        </button>
      }
    </div>

    @switch (state()) {
      @case ('loading') {
        <p class="mt-2 text-muted-foreground" role="status">Loading the preview…</p>
      }
      @case ('unavailable') {
        <p
          class="mt-2 flex items-center gap-2 rounded-md border border-dashed p-3 text-muted-foreground"
        >
          <ng-icon name="lucideEyeOff" size="16" aria-hidden="true" />
          No preview: this app has no candidate view for the task type {{ taskType() }}.
        </p>
      }
      @case ('failed') {
        <p
          role="alert"
          class="mt-2 flex items-center gap-2 rounded-md border border-destructive p-3"
        >
          <ng-icon name="lucideCircleAlert" size="16" class="text-destructive" aria-hidden="true" />
          The preview could not be loaded. Reload the page to try again.
        </p>
      }
    }

    <div class="mt-2" [class.hidden]="state() !== 'ready'">
      <ng-container #outlet />
    </div>
  `,
})
export class TaskPreview {
  private readonly registry = inject(CANDIDATE_TASK_TYPES);
  private readonly outlet = viewChild.required('outlet', { read: ViewContainerRef });
  private readonly generation = signal(0);
  private readonly response = signal<unknown>(null);

  /** The task type key whose `candidate` component renders the preview. */
  readonly taskType = input.required<string>();
  /** The candidate part of the content, as the candidate API would send it. */
  readonly content = input.required<unknown>();

  protected readonly state = signal<PreviewState>('loading');

  constructor() {
    effect((onCleanup) => {
      const taskType = this.taskType();
      const outlet = this.outlet();
      this.generation();
      let current = true;
      onCleanup(() => {
        current = false;
        outlet.clear();
      });
      untracked(() => void this.render(taskType, outlet, () => current));
    });
  }

  protected reset(): void {
    this.generation.update((generation) => generation + 1);
  }

  private async render(
    taskType: string,
    outlet: ViewContainerRef,
    isCurrent: () => boolean,
  ): Promise<void> {
    this.response.set(null);
    const entry = this.registry.find(taskType);
    if (!entry) {
      this.state.set('unavailable');
      return;
    }

    this.state.set('loading');
    let component;
    try {
      component = await entry.loadComponent();
    } catch {
      if (isCurrent()) {
        this.state.set('failed');
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
    this.state.set('ready');
  }
}
