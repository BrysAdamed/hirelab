import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import {
  staffHubEvents,
  staffHubPath,
  type CandidatePresence,
  type StaffHubEvents,
  type StaffHubMethods,
} from '@hirelab/api-client/staff';
import { injectLiveHub } from '@hirelab/core/live';

/** The latest response revision the hub announced for a session task. */
export interface ResponseUpdate {
  readonly sessionTaskId: string;
  readonly revision: number;
}

/**
 * Follows one session live through the staff hub (ADR 0006): it subscribes to the session, asks the page to refetch
 * when the session or its queue changed, and keeps the latest response update and the candidate's presence. After
 * every (re)connect it subscribes again and asks for a refetch, since messages sent meanwhile are lost. Provide it in
 * the session page.
 */
@Injectable()
export class SessionLiveUpdates {
  private sessionId: string | null = null;
  private changed: () => void = () => undefined;
  private started = false;

  private readonly hub = injectLiveHub<StaffHubMethods, StaffHubEvents>(staffHubPath, {
    events: staffHubEvents,
    handlers: {
      TaskQueueChanged: (sessionId) => this.forWatched(sessionId, () => this.changed()),
      SessionStateChanged: (sessionId) => this.forWatched(sessionId, () => this.changed()),
      ResponseUpdated: (sessionId, sessionTaskId, revision) =>
        this.forWatched(sessionId, () => this.responseUpdate.set({ sessionTaskId, revision })),
      CandidatePresenceChanged: (sessionId, presence) =>
        this.forWatched(sessionId, () => this.presence.set(presence)),
    },
    connected: () => this.subscribe(),
  });

  readonly status = this.hub.status;
  /** The candidate's presence, once the subscription returned it. */
  readonly presence = signal<CandidatePresence | null>(null);
  readonly responseUpdate = signal<ResponseUpdate | null>(null);
  /** Raised after every (re)subscription: whatever the page shows may have changed while it wasn't subscribed. */
  readonly resyncs = signal(0);

  constructor() {
    inject(DestroyRef).onDestroy(() => void this.hub.stop());
  }

  /** Follows `sessionId` instead of the previous session, calling `changed` whenever it may have changed. */
  watch(sessionId: string, changed: () => void): void {
    const previous = this.sessionId;
    this.sessionId = sessionId;
    this.changed = changed;
    this.presence.set(null);
    this.responseUpdate.set(null);

    if (!this.started) {
      this.started = true;
      this.hub.start();
    } else if (this.hub.status() === 'connected' && previous !== sessionId) {
      if (previous) {
        void this.hub.invoke('UnsubscribeFromSession', previous).catch(() => undefined);
      }
      void this.subscribe().catch(() => undefined);
    }
  }

  private async subscribe(): Promise<void> {
    const sessionId = this.sessionId;
    if (!sessionId) {
      return;
    }

    const presence = await this.hub.invoke('SubscribeToSession', sessionId);
    if (sessionId === this.sessionId) {
      this.presence.set(presence);
      this.resyncs.update((resyncs) => resyncs + 1);
      this.changed();
    }
  }

  private forWatched(sessionId: string, handle: () => void): void {
    if (sessionId === this.sessionId) {
      handle();
    }
  }
}
