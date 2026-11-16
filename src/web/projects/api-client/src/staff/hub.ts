/**
 * This file was generated from hubs/staff.json by scripts/api-client.mjs.
 * Do not make direct changes to the file.
 */

/** Where the hub is mapped. */
export const staffHubPath = '/hubs/staff';

export interface CandidatePresence {
  connected: boolean;
  connections: number;
  lastSeenAt: string | null;
}

/** The methods a client invokes on the hub: their arguments and what they return. */
export interface StaffHubMethods {
  SubscribeToSession: { args: [sessionId: string]; returns: CandidatePresence };
  UnsubscribeFromSession: { args: [sessionId: string]; returns: void };
}

/** The messages the hub sends to a client, with their arguments. */
export interface StaffHubEvents {
  TaskQueueChanged: [sessionId: string, queueRevision: number];
  SessionStateChanged: [sessionId: string];
  ResponseUpdated: [sessionId: string, sessionTaskId: string, revision: number];
  CandidatePresenceChanged: [sessionId: string, presence: CandidatePresence];
}

/** The names of the messages in {@link StaffHubEvents}. */
export const staffHubEvents = ['TaskQueueChanged', 'SessionStateChanged', 'ResponseUpdated', 'CandidatePresenceChanged'] as const satisfies readonly (keyof StaffHubEvents)[];
