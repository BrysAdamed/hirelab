import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideConstruction } from '@ng-icons/lucide';
import { staffPageOf } from '../shell/staff-page';

/** Stands in for a page whose feature hasn't been built yet. */
@Component({
  selector: 'hl-placeholder-page',
  imports: [NgIcon],
  providers: [provideIcons({ lucideConstruction })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="text-xl font-semibold">{{ label }}</h1>
    <div
      class="mt-3 flex flex-col items-center gap-2 rounded-md border border-dashed p-6 text-center text-muted-foreground"
    >
      <ng-icon name="lucideConstruction" size="20" aria-hidden="true" />
      <p>This page isn't built yet.</p>
    </div>
  `,
})
export class PlaceholderPage {
  protected readonly label =
    staffPageOf(inject(ActivatedRoute).routeConfig)?.nav?.label ?? 'Coming soon';
}
