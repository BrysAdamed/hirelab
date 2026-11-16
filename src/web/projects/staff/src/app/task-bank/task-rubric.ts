import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideTriangleAlert } from '@ng-icons/lucide';
import { HlmBadge } from '@hirelab/ui/helm/badge';
import type { CheckCriterion, Rubric, RubricCriterion, ScaleCriterion } from '../api/staff-api';

type CriterionView =
  { kind: 'check'; criterion: CheckCriterion } | { kind: 'scale'; criterion: ScaleCriterion };

function viewOf(criterion: RubricCriterion): CriterionView {
  return 'expected' in criterion ? { kind: 'check', criterion } : { kind: 'scale', criterion };
}

/** A task's rubric: `scale` criteria with their weight and levels, `check` criteria with their expected answer. */
@Component({
  selector: 'hl-task-rubric',
  imports: [NgIcon, HlmBadge],
  providers: [provideIcons({ lucideTriangleAlert })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ol class="flex flex-col gap-3">
      @for (view of criteria(); track view.criterion.id) {
        <li class="flex flex-col gap-1">
          @switch (view.kind) {
            @case ('check') {
              <div class="flex flex-wrap items-center gap-2">
                <span class="font-medium">{{ view.criterion.title }}</span>
                <span class="text-muted-foreground">
                  Yes / No, expected {{ view.criterion.expected === 'yes' ? 'Yes' : 'No' }}
                </span>
                @if (view.criterion.critical) {
                  <span hlmBadge variant="destructive">
                    <ng-icon name="lucideTriangleAlert" aria-hidden="true" />
                    Critical
                  </span>
                }
              </div>
              <p>{{ view.criterion.description }}</p>
            }
            @case ('scale') {
              <div class="flex flex-wrap items-center gap-2">
                <span class="font-medium">{{ view.criterion.title }}</span>
                <span class="text-muted-foreground"
                  >Scale 0–4, weight {{ view.criterion.weight }}</span
                >
              </div>
              <p>{{ view.criterion.description }}</p>
              @if (view.criterion.levels?.length) {
                <dl class="grid grid-cols-[auto_1fr] gap-x-2 text-muted-foreground">
                  @for (level of view.criterion.levels; track level.score) {
                    <dt class="font-mono">{{ level.score }}</dt>
                    <dd>{{ level.description }}</dd>
                  }
                </dl>
              }
            }
          }
        </li>
      }
    </ol>
  `,
})
export class TaskRubric {
  readonly rubric = input.required<Rubric>();

  protected readonly criteria = computed(() => this.rubric().criteria.map(viewOf));
}
