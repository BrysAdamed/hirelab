import type { Route } from '@angular/router';
import type { StaffRole } from '../api/staff-api';

export const ALL_ROLES: readonly StaffRole[] = ['Admin', 'Manager', 'Interviewer'];

/** A page in the app shell: who may open it and, optionally, its navigation item. */
export interface StaffPage {
  roles: readonly StaffRole[];
  nav?: {
    label: string;
    /** An SVG from `@ng-icons/lucide`, for example `lucideUsers`. */
    icon: string;
  };
}

const staffPageKey = 'staffPage';

/**
 * Route data for a page in the app shell. `roleGuard` lets only `roles` open it, and the shell shows
 * `nav` in the navigation of those roles, in route order.
 */
export function staffPage(page: StaffPage): Route['data'] {
  return { [staffPageKey]: page };
}

export function staffPageOf(route: Route | null | undefined): StaffPage | undefined {
  return route?.data?.[staffPageKey] as StaffPage | undefined;
}
