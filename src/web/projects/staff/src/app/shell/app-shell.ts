import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { ThemeSwitcher } from '@hirelab/ui';
import { AuthService } from '../auth/auth.service';
import { staffPageOf } from './staff-page';
import { UserMenu } from './user-menu';

interface NavItem {
  path: string;
  label: string;
  icon: string;
}

/** The signed-in layout: header with the user menu, navigation for the user's role, and the page. */
@Component({
  selector: 'hl-app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NgIcon, ThemeSwitcher, UserMenu],
  templateUrl: './app-shell.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShell {
  private readonly auth = inject(AuthService);
  private readonly pages = inject(ActivatedRoute).routeConfig?.children ?? [];

  protected readonly navItems = computed<NavItem[]>(() => {
    const role = this.auth.user()?.role;
    return this.pages.flatMap((route) => {
      const page = staffPageOf(route);
      return page?.nav && role && page.roles.includes(role)
        ? [{ path: `/${route.path ?? ''}`, label: page.nav.label, icon: page.nav.icon }]
        : [];
    });
  });
}
