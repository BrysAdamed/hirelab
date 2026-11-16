import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideGlobe, lucidePackage, lucideUsersRound } from '@ng-icons/lucide';
import { HlmBadge } from '@hirelab/ui/helm/badge';
import type { BankVisibility } from '../api/staff-api';

const badges: Record<BankVisibility, { icon: string; label: string }> = {
  Team: { icon: 'lucideUsersRound', label: 'Team' },
  Global: { icon: 'lucideGlobe', label: 'Global' },
  System: { icon: 'lucidePackage', label: 'System' },
};

/** Who sees a task in the task bank, as an icon and a label: its team only, every team, or a system task. */
@Component({
  selector: 'hl-task-visibility-badge',
  imports: [NgIcon, HlmBadge],
  providers: [provideIcons({ lucideGlobe, lucidePackage, lucideUsersRound })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span hlmBadge [variant]="visibility() === 'System' ? 'outline' : 'secondary'">
      <ng-icon [name]="badge().icon" aria-hidden="true" />
      {{ badge().label }}
    </span>
  `,
})
export class TaskVisibilityBadge {
  readonly visibility = input.required<BankVisibility>();

  protected readonly badge = computed(() => badges[this.visibility()]);
}
