import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideContrast, lucideMonitor, lucideMoon, lucideSun } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { type ContrastPreference, type ThemePreference, ThemeService } from './theme.service';

interface Option<T> {
  value: T;
  label: string;
  icon: string;
}

/** Two switches: theme (system / light / dark) and high contrast (system / on / off). */
@Component({
  selector: 'hl-theme-switcher',
  imports: [HlmButton, NgIcon],
  providers: [provideIcons({ lucideContrast, lucideMonitor, lucideMoon, lucideSun })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-wrap items-center gap-3' },
  template: `
    <div role="group" aria-label="Theme" class="flex items-center gap-1">
      @for (option of themeOptions; track option.value) {
        <button
          hlmBtn
          type="button"
          size="sm"
          [variant]="themeService.themePreference() === option.value ? 'secondary' : 'ghost'"
          [attr.aria-pressed]="themeService.themePreference() === option.value"
          (click)="themeService.setThemePreference(option.value)"
        >
          <ng-icon [name]="option.icon" aria-hidden="true" />
          {{ option.label }}
        </button>
      }
    </div>
    <div role="group" aria-label="High contrast" class="flex items-center gap-1">
      <ng-icon name="lucideContrast" aria-hidden="true" class="text-muted-foreground" />
      <span class="text-muted-foreground">High contrast</span>
      @for (option of contrastOptions; track option.value) {
        <button
          hlmBtn
          type="button"
          size="sm"
          [variant]="themeService.contrastPreference() === option.value ? 'secondary' : 'ghost'"
          [attr.aria-pressed]="themeService.contrastPreference() === option.value"
          (click)="themeService.setContrastPreference(option.value)"
        >
          {{ option.label }}
        </button>
      }
    </div>
  `,
})
export class ThemeSwitcher {
  protected readonly themeService = inject(ThemeService);

  protected readonly themeOptions: readonly Option<ThemePreference>[] = [
    { value: 'system', label: 'System', icon: 'lucideMonitor' },
    { value: 'light', label: 'Light', icon: 'lucideSun' },
    { value: 'dark', label: 'Dark', icon: 'lucideMoon' },
  ];

  protected readonly contrastOptions: readonly Omit<Option<ContrastPreference>, 'icon'>[] = [
    { value: 'system', label: 'System' },
    { value: 'on', label: 'On' },
    { value: 'off', label: 'Off' },
  ];
}
