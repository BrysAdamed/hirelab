import { Injectable, InjectionToken, inject } from '@angular/core';

/** How often {@link InterviewUpdates} asks for changes, in milliseconds. */
export const INTERVIEW_POLL_INTERVAL = new InjectionToken<number>('INTERVIEW_POLL_INTERVAL', {
  providedIn: 'root',
  factory: () => 5_000,
});

/**
 * Tells the interview page when its interview may have changed: the session went Live, a task was marked done or the
 * session finished. For now it polls; the candidate hub (HLB-54) replaces the polling with its thin events, and the
 * page keeps refetching over HTTP either way (ADR 0006).
 */
@Injectable({ providedIn: 'root' })
export class InterviewUpdates {
  private readonly interval = inject(INTERVIEW_POLL_INTERVAL);

  /** Calls `onChange` whenever the interview may have changed, until the returned function is called. */
  watch(onChange: () => void): () => void {
    const timer = setInterval(onChange, this.interval);
    return () => clearInterval(timer);
  }
}
