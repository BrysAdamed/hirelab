import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideWifi, lucideWifiOff } from '@ng-icons/lucide';
import { HlmBadge } from '@hirelab/ui/helm/badge';

/** Whether the candidate is connected, through how many connections, and when they were last seen (ISO 8601). */
export interface Presence {
  readonly connected: boolean;
  readonly connections: number;
  readonly lastSeenAt: string | null;
}

/**
 * The candidate's presence in a live session (ADR 0006): online with the number of open connections when there is
 * more than one, or offline with when they were last seen. Icon and label, never colour alone.
 */
@Component({
  selector: 'hl-presence-indicator',
  imports: [DatePipe, NgIcon, HlmBadge],
  providers: [provideIcons({ lucideWifi, lucideWifiOff })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @let current = presence();
    @if (current.connected) {
      <span hlmBadge variant="outline">
        <ng-icon name="lucideWifi" aria-hidden="true" />
        Candidate online
        @if (current.connections > 1) {
          · {{ current.connections }} connections
        }
      </span>
    } @else {
      <span hlmBadge variant="outline" class="text-muted-foreground">
        <ng-icon name="lucideWifiOff" aria-hidden="true" />
        @if (current.lastSeenAt; as lastSeenAt) {
          Candidate offline · last seen {{ lastSeenAt | date: 'short' }}
        } @else {
          Candidate not connected yet
        }
      </span>
    }
  `,
})
export class PresenceIndicator {
  readonly presence = input.required<Presence>();
}
