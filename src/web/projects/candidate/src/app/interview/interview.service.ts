import { Injectable, inject } from '@angular/core';
import { CANDIDATE_API, type Interview } from '../api/candidate-api';

/** Why the candidate can't see their interview. */
export type InterviewUnavailable =
  /** The session has finished (`CandidateSession.Ended`). */
  | 'ended'
  /** The link was regenerated or its session deleted (`CandidateLink.Invalid`). */
  | 'invalid-link'
  /** No candidate cookie: the page was opened without a link, or the cookie expired. */
  | 'no-access'
  /** Too many link exchanges from this address. */
  | 'rate-limited'
  /** The API couldn't be reached or failed. */
  | 'failed';

export type InterviewResult =
  | { readonly ok: true; readonly interview: Interview }
  | { readonly ok: false; readonly reason: InterviewUnavailable };

const unavailableByCode: Readonly<Record<string, InterviewUnavailable>> = {
  'CandidateSession.Ended': 'ended',
  'CandidateLink.Invalid': 'invalid-link',
};

/**
 * The candidate's access to their interview (ADR 0008). Opening a link hands its token over with {@link holdToken},
 * so the address bar can drop it at once; {@link open} then exchanges it for the candidate cookie and reads the
 * interview, which {@link refresh} reads again later.
 */
@Injectable({ providedIn: 'root' })
export class InterviewService {
  private readonly api = inject(CANDIDATE_API);
  private heldToken: string | null = null;

  /** Keeps the token of the opened link until {@link open} exchanges it. */
  holdToken(token: string): void {
    this.heldToken = token;
  }

  /** Exchanges a held link token for the candidate cookie, if there is one, and reads the interview. */
  async open(): Promise<InterviewResult> {
    const token = this.heldToken;
    this.heldToken = null;
    if (token !== null) {
      const exchanged = await this.exchange(token);
      if (exchanged) {
        return exchanged;
      }
    }
    return this.refresh();
  }

  /** Reads the interview with the candidate cookie. */
  async refresh(): Promise<InterviewResult> {
    try {
      const { data, error, response } = await this.api.GET('/api/candidate/session');
      return data ? { ok: true, interview: data } : unavailable(response.status, error);
    } catch {
      return { ok: false, reason: 'failed' };
    }
  }

  private async exchange(token: string): Promise<InterviewResult | null> {
    try {
      await this.api.GET('/api/candidate/access/antiforgery');
      const { error, response } = await this.api.POST('/api/candidate/access', { body: { token } });
      return response.ok ? null : unavailable(response.status, error);
    } catch {
      return { ok: false, reason: 'failed' };
    }
  }
}

function unavailable(status: number, error: unknown): InterviewResult {
  const code = (error as { code?: unknown } | undefined)?.code;
  const byCode = typeof code === 'string' ? unavailableByCode[code] : undefined;
  const reason: InterviewUnavailable =
    byCode ?? (status === 401 ? 'no-access' : status === 429 ? 'rate-limited' : 'failed');
  return { ok: false, reason };
}
