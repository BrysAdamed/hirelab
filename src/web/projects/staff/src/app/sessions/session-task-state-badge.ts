import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircle, lucideCircleCheck, lucideCircleSlash, lucidePlay } from '@ng-icons/lucide';
import { HlmBadge } from '@hirelab/ui/helm/badge';
import type { SessionTaskState } from '../api/staff-api';

/** A session task's state as an icon and a label; a Pending task of a finished session is "not reached". */
@Component({
  selector: 'hl-session-task-state-badge',
  imports: [NgIcon, HlmBadge],
  providers: [provideIcons({ lucideCircle, lucideCircleCheck, lucideCircleSlash, lucidePlay })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @switch (shown()) {
      @case ('Pending') {
        <span hlmBadge variant="outline">
          <ng-icon name="lucideCircle" aria-hidden="true" />
          Pending
        </span>
      }
      @case ('Current') {
        <span hlmBadge>
          <ng-icon name="lucidePlay" aria-hidden="true" />
          Current
        </span>
      }
      @case ('Done') {
        <span hlmBadge variant="secondary">
          <ng-icon name="lucideCircleCheck" aria-hidden="true" />
          Done
        </span>
      }
      @case ('NotReached') {
        <span hlmBadge variant="outline" class="text-muted-foreground">
          <ng-icon name="lucideCircleSlash" aria-hidden="true" />
          Not reached
        </span>
      }
    }
  `,
})
export class SessionTaskStateBadge {
  readonly state = input.required<SessionTaskState>();
  /** Whether the session has finished, which turns a Pending task into a "not reached" one. */
  readonly sessionFinished = input(false);

  protected readonly shown = computed(() =>
    this.sessionFinished() && this.state() === 'Pending' ? 'NotReached' : this.state(),
  );
}
