import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  Directive,
  TemplateRef,
  contentChild,
  inject,
  input,
} from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideBan, lucideCircleCheck } from '@ng-icons/lucide';
import { HlmBadge } from '@hirelab/ui/helm/badge';
import type { UserSummary } from '../api/staff-api';

/** The context of a row-actions template: `let-user` is the row's user. */
export interface UserRowContext {
  $implicit: UserSummary;
}

/**
 * Marks the template rendered in each row's actions cell, for example
 * `<ng-template hlUserRowActions let-user>…</ng-template>`.
 */
@Directive({ selector: 'ng-template[hlUserRowActions]' })
export class UserRowActions {
  readonly template = inject<TemplateRef<UserRowContext>>(TemplateRef);

  static ngTemplateContextGuard(
    _directive: UserRowActions,
    context: unknown,
  ): context is UserRowContext {
    return typeof context === 'object';
  }
}

/** Staff users with their role, team and state. The page puts its actions into each row through `hlUserRowActions`. */
@Component({
  selector: 'hl-user-table',
  imports: [NgTemplateOutlet, NgIcon, HlmBadge],
  providers: [provideIcons({ lucideBan, lucideCircleCheck })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (users().length === 0) {
      <p class="rounded-md border border-dashed p-3 text-center text-muted-foreground">
        {{ emptyText() }}
      </p>
    } @else {
      <div class="overflow-x-auto rounded-md border">
        <table class="w-full text-left">
          <caption class="sr-only">
            {{
              caption()
            }}
          </caption>
          <thead class="border-b bg-muted text-muted-foreground">
            <tr>
              <th scope="col" class="px-3 py-2 font-medium">User</th>
              <th scope="col" class="px-3 py-2 font-medium">Role</th>
              @if (showTeam()) {
                <th scope="col" class="px-3 py-2 font-medium">Team</th>
              }
              <th scope="col" class="px-3 py-2 font-medium">State</th>
              <th scope="col" class="px-3 py-2"><span class="sr-only">Actions</span></th>
            </tr>
          </thead>
          <tbody>
            @for (user of users(); track user.id) {
              <tr class="border-b last:border-b-0">
                <td class="px-3 py-2">
                  <div>{{ user.displayName }}</div>
                  <div class="text-muted-foreground">{{ user.email }}</div>
                </td>
                <td class="px-3 py-2">{{ user.role }}</td>
                @if (showTeam()) {
                  <td class="px-3 py-2">{{ user.team?.name }}</td>
                }
                <td class="px-3 py-2">
                  @if (user.isActive) {
                    <span hlmBadge variant="secondary">
                      <ng-icon name="lucideCircleCheck" aria-hidden="true" />
                      Active
                    </span>
                  } @else {
                    <span hlmBadge variant="outline">
                      <ng-icon name="lucideBan" aria-hidden="true" />
                      Deactivated
                    </span>
                  }
                </td>
                <td class="px-3 py-2">
                  @if (rowActions(); as actions) {
                    <div class="flex flex-wrap justify-end gap-2">
                      <ng-container
                        [ngTemplateOutlet]="actions.template"
                        [ngTemplateOutletContext]="{ $implicit: user }"
                      />
                    </div>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
})
export class UserTable {
  readonly users = input.required<readonly UserSummary[]>();
  readonly caption = input.required<string>();
  readonly showTeam = input(false);
  readonly emptyText = input('No users yet.');

  protected readonly rowActions = contentChild(UserRowActions);
}
