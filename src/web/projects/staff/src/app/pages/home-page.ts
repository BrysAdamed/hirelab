import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'hl-home-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (auth.user(); as user) {
      <h1 class="text-xl font-semibold">Welcome, {{ user.displayName }}</h1>
      <p class="mt-1 text-muted-foreground">{{ signedInAs() }}</p>
    }
  `,
})
export class HomePage {
  protected readonly auth = inject(AuthService);

  protected readonly signedInAs = computed(() => {
    const user = this.auth.user();
    if (!user) {
      return '';
    }
    const role = user.role.toLowerCase();
    return user.team
      ? `You are signed in as ${role} in ${user.team.name}.`
      : `You are signed in as ${role}.`;
  });
}
