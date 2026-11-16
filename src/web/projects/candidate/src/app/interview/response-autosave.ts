import { Injectable, InjectionToken, inject, signal } from '@angular/core';
import { CANDIDATE_API } from '../api/candidate-api';

/** How long {@link ResponseAutosave} waits after the last change before it saves, in milliseconds. */
export const RESPONSE_AUTOSAVE_DELAY = new InjectionToken<number>('RESPONSE_AUTOSAVE_DELAY', {
  providedIn: 'root',
  factory: () => 1_000,
});

/** How long {@link ResponseAutosave} waits before it tries a failed save again, in milliseconds. */
export const RESPONSE_AUTOSAVE_RETRY_DELAY = new InjectionToken<number>(
  'RESPONSE_AUTOSAVE_RETRY_DELAY',
  { providedIn: 'root', factory: () => 5_000 },
);

/**
 * What the candidate sees about saving: nothing yet (`idle`), `saving`, `saved`, `failed` (it tries again), `rejected`
 * (the server won't take the response as it is) or `stale` (another tab saved a newer response, which was loaded).
 */
export type AutosaveStatus = 'idle' | 'saving' | 'saved' | 'failed' | 'rejected' | 'stale';

/**
 * Why the server refused a save in a way the page must act on: another tab saved first (`stale`), or the task is no
 * longer the one to answer (`moved-on`: it was marked done, the session finished or the cookie stopped working).
 */
export type AutosaveRefusal = 'stale' | 'moved-on';

type SaveOutcome =
  | { readonly kind: 'saved'; readonly revision: number }
  | { readonly kind: 'refused'; readonly refusal: AutosaveRefusal }
  | { readonly kind: 'rejected' }
  | { readonly kind: 'failed' };

const staleCodes = new Set(['Response.StaleRevision', 'Concurrency.ChangedMeanwhile']);

/**
 * Saves the candidate's response to the current task (ADR 0031): the whole response, debounced, one save at a time,
 * each naming the revision the previous one returned. A refusal is handed to the page through {@link onRefused}, which
 * refetches the interview. Provide it in the component that owns the response.
 */
@Injectable()
export class ResponseAutosave {
  private readonly api = inject(CANDIDATE_API);
  private readonly delay = inject(RESPONSE_AUTOSAVE_DELAY);
  private readonly retryDelay = inject(RESPONSE_AUTOSAVE_RETRY_DELAY);

  private sessionTaskId: string | null = null;
  private savedRevision = 0;
  private unsaved: { readonly response: unknown } | null = null;
  private timer: ReturnType<typeof setTimeout> | undefined;
  private saving: Promise<void> | null = null;
  private refused: (refusal: AutosaveRefusal) => void = () => undefined;

  readonly status = signal<AutosaveStatus>('idle');

  /** The revision of the last response saved or loaded for the tracked task. */
  get revision(): number {
    return this.savedRevision;
  }

  /** Whether a change hasn't been saved yet. */
  get dirty(): boolean {
    return this.unsaved !== null || this.saving !== null;
  }

  /** Sets what to do when the server refuses a save. */
  onRefused(refused: (refusal: AutosaveRefusal) => void): void {
    this.refused = refused;
  }

  /**
   * Follows `sessionTaskId` from its stored `revision`, dropping changes to the previous task that weren't saved: the
   * server refuses them once another task is current. `null` stops saving.
   */
  track(sessionTaskId: string | null, revision: number, status: AutosaveStatus = 'idle'): void {
    clearTimeout(this.timer);
    this.sessionTaskId = sessionTaskId;
    this.savedRevision = revision;
    this.unsaved = null;
    this.status.set(status);
  }

  /** Saves `response` once the candidate pauses. */
  change(response: unknown): void {
    if (this.sessionTaskId === null) {
      return;
    }
    this.unsaved = { response };
    this.schedule(this.delay);
  }

  /** Saves a pending change now, for example when the page is hidden. */
  flush(): Promise<void> {
    clearTimeout(this.timer);
    return this.save();
  }

  /** Stops a pending save. */
  stop(): void {
    clearTimeout(this.timer);
    this.sessionTaskId = null;
    this.unsaved = null;
  }

  private schedule(delay: number): void {
    clearTimeout(this.timer);
    this.timer = setTimeout(() => void this.save(), delay);
  }

  private save(): Promise<void> {
    if (!this.saving && this.unsaved) {
      this.saving = this.drain().finally(() => (this.saving = null));
    }
    return this.saving ?? Promise.resolve();
  }

  private async drain(): Promise<void> {
    while (this.unsaved && this.sessionTaskId !== null) {
      const sessionTaskId = this.sessionTaskId;
      const { response } = this.unsaved;
      this.unsaved = null;
      this.status.set('saving');

      const outcome = await this.send(sessionTaskId, response, this.savedRevision);
      if (sessionTaskId !== this.sessionTaskId) {
        return;
      }

      switch (outcome.kind) {
        case 'saved':
          this.savedRevision = outcome.revision;
          break;
        case 'refused':
          this.unsaved = null;
          this.status.set(outcome.refusal === 'stale' ? 'stale' : 'idle');
          this.refused(outcome.refusal);
          return;
        case 'rejected':
          this.status.set('rejected');
          return;
        case 'failed':
          this.unsaved ??= { response };
          this.status.set('failed');
          this.schedule(this.retryDelay);
          return;
      }
    }
    this.status.set('saved');
  }

  private async send(sessionTaskId: string, response: unknown, revision: number): Promise<SaveOutcome> {
    try {
      const put = () =>
        this.api.PUT('/api/candidate/session/response', {
          body: { sessionTaskId, response, revision },
        });
      let result = await put();
      if (result.response.status === 400 && codeOf(result.error) === 'Antiforgery.InvalidToken') {
        await this.api.GET('/api/candidate/access/antiforgery');
        result = await put();
      }

      const { data, error, response: http } = result;
      if (data) {
        return { kind: 'saved', revision: data.revision };
      }
      if (http.status === 409) {
        return { kind: 'refused', refusal: staleCodes.has(codeOf(error) ?? '') ? 'stale' : 'moved-on' };
      }
      if (http.status === 401 || http.status === 404) {
        return { kind: 'refused', refusal: 'moved-on' };
      }
      return http.status === 400 || http.status === 413 ? { kind: 'rejected' } : { kind: 'failed' };
    } catch {
      return { kind: 'failed' };
    }
  }
}

function codeOf(error: unknown): string | undefined {
  const code = (error as { code?: unknown } | undefined)?.code;
  return typeof code === 'string' ? code : undefined;
}
