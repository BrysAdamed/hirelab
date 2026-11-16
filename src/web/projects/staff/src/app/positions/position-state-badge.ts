import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleDot, lucideLock } from '@ng-icons/lucide';
import { HlmBadge } from '@hirelab/ui/helm/badge';
import type { JobPositionState } from '../api/staff-api';

/** A job position's state as an icon and a label. */
@Component({
  selector: 'hl-position-state-badge',
  imports: [NgIcon, HlmBadge],
  providers: [provideIcons({ lucideCircleDot, lucideLock })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (state() === 'Open') {
      <span hlmBadge variant="secondary">
        <ng-icon name="lucideCircleDot" aria-hidden="true" />
        Open
      </span>
    } @else {
      <span hlmBadge variant="outline">
        <ng-icon name="lucideLock" aria-hidden="true" />
        Closed
      </span>
    }
  `,
})
export class PositionStateBadge {
  readonly state = input.required<JobPositionState>();
}
