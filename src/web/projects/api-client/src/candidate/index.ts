import createClient, { type Client, type ClientOptions } from 'openapi-fetch';
import type { components, paths } from './schema';

export type { components, paths } from './schema';

export type ProblemDetails = components['schemas']['ProblemDetails'];

export type CandidateApiClient = Client<paths>;

/**
 * Creates a typed client for the `/api/candidate` endpoints. Requests go to the same origin and
 * carry the candidate session cookie; failed responses resolve with `error` typed as
 * {@link ProblemDetails}.
 */
export function createCandidateApiClient(
  options: Pick<ClientOptions, 'baseUrl' | 'fetch'> = {},
): CandidateApiClient {
  return createClient<paths>({ baseUrl: '', ...options, credentials: 'same-origin' });
}
