import createClient, { type Client, type ClientOptions } from 'openapi-fetch';
import type { components, paths } from './schema';

export type { components, paths } from './schema';
export * from './hub';

export type ProblemDetails = components['schemas']['ProblemDetails'];

export type StaffApiClient = Client<paths>;

/**
 * Creates a typed client for the `/api/staff` endpoints. Requests go to the same origin and carry
 * the staff session cookie; failed responses resolve with `error` typed as {@link ProblemDetails}.
 */
export function createStaffApiClient(
  options: Pick<ClientOptions, 'baseUrl' | 'fetch'> = {},
): StaffApiClient {
  return createClient<paths>({ baseUrl: '', ...options, credentials: 'same-origin' });
}
