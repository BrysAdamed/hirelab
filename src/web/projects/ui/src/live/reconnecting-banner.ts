import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideRefreshCw } from '@ng-icons/lucide';

/**
 * Says that live updates are paused while the real-time connection reconnects (ADR 0006). The live region is always
 * there, so screen readers announce the message when it appears.
 */
@Component({
  selector: 'hl-reconnecting-banner',
  imports: [NgIcon],
  providers: [provideIcons({ lucideRefreshCw })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div aria-live="polite" aria-atomic="true">
      @if (reconnecting()) {
        <p class="flex items-center gap-2 rounded-md bg-warning px-3 py-2 text-warning-foreground">
          <ng-icon
            name="lucideRefreshCw"
            size="16"
            class="shrink-0 motion-safe:animate-spin"
            aria-hidden="true"
          />
          Reconnecting… {{ detail() }}
        </p>
      }
    </div>
  `,
})
export class ReconnectingBanner {
  readonly reconnecting = input.required<boolean>();
  /** What the user should know while live updates are paused. */
  readonly detail = input('Live updates are paused.');
}
