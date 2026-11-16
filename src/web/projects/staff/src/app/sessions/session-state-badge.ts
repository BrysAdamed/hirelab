import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCalendarClock, lucideCircleCheck, lucideRadio } from '@ng-icons/lucide';
import { HlmBadge } from '@hirelab/ui/helm/badge';
import type { InterviewSessionState } from '../api/staff-api';

/** An interview session's state as an icon and a label. */
@Component({
  selector: 'hl-session-state-badge',
  imports: [NgIcon, HlmBadge],
  providers: [provideIcons({ lucideCalendarClock, lucideCircleCheck, lucideRadio })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @switch (state()) {
      @case ('Planned') {
        <span hlmBadge variant="outline">
          <ng-icon name="lucideCalendarClock" aria-hidden="true" />
          Planned
        </span>
      }
      @case ('Live') {
        <span hlmBadge>
          <ng-icon name="lucideRadio" aria-hidden="true" />
          Live
        </span>
      }
      @case ('Finished') {
        <span hlmBadge variant="secondary">
          <ng-icon name="lucideCircleCheck" aria-hidden="true" />
          Finished
        </span>
      }
    }
  `,
})
export class SessionStateBadge {
  readonly state = input.required<InterviewSessionState>();
}
