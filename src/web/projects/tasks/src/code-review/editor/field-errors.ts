import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import type { TaskContentErrors } from '@hirelab/tasks/editor';

/** The messages of the given keys, in key order. */
export function messagesOf(errors: TaskContentErrors, ...keys: string[]): readonly string[] {
  return keys.flatMap((key) => errors[key] ?? []);
}

/** The `aria-describedby` of a field: its hint and, when it has errors, its error list. */
export function describedBy(
  id: string,
  messages: readonly string[],
  hasHint = false,
): string | null {
  const ids = [hasHint ? `${id}-hint` : null, messages.length > 0 ? `${id}-errors` : null];
  return ids.filter((value) => value !== null).join(' ') || null;
}

@Component({
  selector: 'hl-field-errors',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'contents' },
  template: `
    @if (messages().length > 0) {
      <ul class="text-destructive text-sm" [id]="fieldId() + '-errors'">
        @for (message of messages(); track $index) {
          <li>{{ message }}</li>
        }
      </ul>
    }
  `,
})
export class FieldErrors {
  readonly fieldId = input.required<string>();
  readonly messages = input.required<readonly string[]>();
}
