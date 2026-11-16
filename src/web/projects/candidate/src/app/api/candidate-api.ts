import { DOCUMENT, InjectionToken, inject } from '@angular/core';
import { CANDIDATE_ANTIFORGERY_COOKIE, antiforgeryMiddleware } from '@hirelab/core';
import {
  type CandidateApiClient,
  type components,
  createCandidateApiClient,
} from '@hirelab/api-client/candidate';

export type Interview = components['schemas']['InterviewResponse'];
export type InterviewStage = components['schemas']['InterviewStage'];
export type CurrentTask = components['schemas']['CurrentTaskResponse'];

/** The `fetch` the candidate API client uses. Tests replace it. */
export const CANDIDATE_API_FETCH = new InjectionToken<typeof fetch>('CANDIDATE_API_FETCH', {
  providedIn: 'root',
  factory: () => (input, init) => globalThis.fetch(input, init),
});

/** The typed candidate API client. It sends the candidate's antiforgery header on unsafe methods (ADR 0007, ADR 0031). */
export const CANDIDATE_API = new InjectionToken<CandidateApiClient>('CANDIDATE_API', {
  providedIn: 'root',
  factory: () => {
    const document = inject(DOCUMENT);
    const client = createCandidateApiClient({
      baseUrl: document.location.origin,
      fetch: inject(CANDIDATE_API_FETCH),
    });
    client.use(antiforgeryMiddleware(document, CANDIDATE_ANTIFORGERY_COOKIE));
    return client;
  },
});
