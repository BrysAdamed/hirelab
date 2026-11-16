import { Injectable, inject, type Signal } from '@angular/core';
import {
  candidateHubEvents,
  candidateHubPath,
  type CandidateHubEvents,
  type CandidateHubMethods,
} from '@hirelab/api-client/candidate';
import {
  LIVE_HUB_CONNECTION_FACTORY,
  LIVE_HUB_RETRY_DELAYS,
  LiveHub,
  type LiveHubStatus,
} from '@hirelab/core/live';

/** What the interview page does when its interview may have changed. */
export interface InterviewWatcher {
  /** The interview may have changed, or updates may have been missed: refetch it. */
  changed(): void;
  /** A response to `sessionTaskId` was saved with `revision`, here or in another tab. */
  responseSaved(sessionTaskId: string, revision: number): void;
}

/** A running watch on the interview. */
export interface InterviewWatch {
  readonly status: Signal<LiveHubStatus>;
  stop(): void;
}

/**
 * Tells the interview page when its interview may have changed, through the candidate hub's thin events (ADR 0006):
 * the session went Live or finished, the queue changed or a response was saved. Messages sent while the connection
 * was down are lost, so it also asks for a refetch whenever the connection opens or drops.
 */
@Injectable({ providedIn: 'root' })
export class InterviewUpdates {
  private readonly connect = inject(LIVE_HUB_CONNECTION_FACTORY);
  private readonly retryDelays = inject(LIVE_HUB_RETRY_DELAYS);

  watch(watcher: InterviewWatcher): InterviewWatch {
    const hub = new LiveHub<CandidateHubMethods, CandidateHubEvents>(
      this.connect(candidateHubPath, this.retryDelays),
      this.retryDelays,
      {
        events: candidateHubEvents,
        handlers: {
          TaskQueueChanged: () => watcher.changed(),
          SessionStateChanged: () => watcher.changed(),
          ResponseUpdated: (sessionTaskId, revision) =>
            watcher.responseSaved(sessionTaskId, revision),
        },
        connected: () => watcher.changed(),
        reconnecting: () => watcher.changed(),
      },
    );
    hub.start();
    return { status: hub.status, stop: () => void hub.stop() };
  }
}
