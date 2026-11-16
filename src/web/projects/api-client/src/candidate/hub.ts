/**
 * This file was generated from hubs/candidate.json by scripts/api-client.mjs.
 * Do not make direct changes to the file.
 */

/** Where the hub is mapped. */
export const candidateHubPath = '/hubs/candidate';

/** The methods a client invokes on the hub: their arguments and what they return. */
export type CandidateHubMethods = Record<never, never>;

/** The messages the hub sends to a client, with their arguments. */
export interface CandidateHubEvents {
  TaskQueueChanged: [];
  SessionStateChanged: [];
  ResponseUpdated: [sessionTaskId: string, revision: number];
}

/** The names of the messages in {@link CandidateHubEvents}. */
export const candidateHubEvents = ['TaskQueueChanged', 'SessionStateChanged', 'ResponseUpdated'] as const satisfies readonly (keyof CandidateHubEvents)[];
