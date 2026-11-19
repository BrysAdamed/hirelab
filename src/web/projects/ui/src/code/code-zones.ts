import { Directive, TemplateRef, inject, input } from '@angular/core';
import type { CodeViewZone } from './code-types';

export interface CodeZoneContext<T> {
  readonly $implicit: CodeViewZone<T>;
}

/**
 * The zones to render under lines of an `hl-code-diff` and the template for each, rendered with the zone as its
 * implicit context: `<ng-template [hlCodeZones]="threads()" let-zone>…</ng-template>`.
 */
@Directive({ selector: 'ng-template[hlCodeZones]' })
export class CodeZones<T> {
  readonly template = inject<TemplateRef<CodeZoneContext<T>>>(TemplateRef);

  readonly zones = input.required<readonly CodeViewZone<T>[]>({ alias: 'hlCodeZones' });

  static ngTemplateContextGuard<T>(
    _directive: CodeZones<T>,
    context: unknown,
  ): context is CodeZoneContext<T> {
    return typeof context === 'object';
  }
}
