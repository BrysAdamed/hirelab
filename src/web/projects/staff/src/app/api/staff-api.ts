import { DOCUMENT, InjectionToken, inject } from '@angular/core';
import { antiforgeryMiddleware } from '@hirelab/core';
import {
  createStaffApiClient,
  type StaffApiClient,
  type components,
} from '@hirelab/api-client/staff';
import type { Middleware } from 'openapi-fetch';

export type CurrentUser = components['schemas']['CurrentUserResponse'];
export type StaffRole = components['schemas']['StaffRole'];
export type Invite = components['schemas']['InviteResponse'];
export type InviteState = components['schemas']['OneTimeLinkState'];
export type IssuedInvite = components['schemas']['IssuedInviteResponse'];
export type InvitePreview = components['schemas']['InvitePreviewResponse'];
export type TeamWithoutManager = components['schemas']['TeamWithoutManagerResponse'];
export type UserSummary = components['schemas']['UserResponse'];
export type TeamSummary = components['schemas']['TeamResponse'];

/** The message of a failed API call: the ProblemDetails detail, or a generic fallback. */
export function problemMessage(
  error: { detail?: string | null; title?: string | null } | undefined,
  fallback: string,
): string {
  return error?.detail ?? error?.title ?? fallback;
}

const authPathPrefix = '/api/staff/auth/';

/** The `fetch` the staff API client uses. Tests replace it. */
export const STAFF_API_FETCH = new InjectionToken<typeof fetch>('STAFF_API_FETCH', {
  providedIn: 'root',
  factory: () => (input, init) => globalThis.fetch(input, init),
});

/** Called when a staff API call other than the auth endpoints answers 401: the session has ended. */
export const SESSION_EXPIRED_HANDLER = new InjectionToken<() => void>('SESSION_EXPIRED_HANDLER');

/** The typed staff API client: sends the antiforgery header and reports ended sessions. */
export const STAFF_API = new InjectionToken<StaffApiClient>('STAFF_API', {
  providedIn: 'root',
  factory: () => {
    const document = inject(DOCUMENT);
    const client = createStaffApiClient({
      baseUrl: document.location.origin,
      fetch: inject(STAFF_API_FETCH),
    });
    const onSessionExpired = inject(SESSION_EXPIRED_HANDLER, { optional: true });
    client.use(antiforgeryMiddleware(document));
    if (onSessionExpired) {
      client.use(sessionExpiryMiddleware(onSessionExpired));
    }
    return client;
  },
});

/** Calls `onSessionExpired` when a call outside the auth endpoints answers 401. */
export function sessionExpiryMiddleware(onSessionExpired: () => void): Middleware {
  return {
    onResponse({ request, response }) {
      if (response.status === 401 && !new URL(request.url).pathname.startsWith(authPathPrefix)) {
        onSessionExpired();
      }
      return undefined;
    },
  };
}
