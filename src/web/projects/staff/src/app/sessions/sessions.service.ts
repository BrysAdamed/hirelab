import { Injectable, inject } from '@angular/core';
import { type ApiResult, apiFailure } from '../api/api-result';
import {
  type CandidateSession,
  type CreatedSession,
  type InterviewSession,
  STAFF_API,
  type TeamSession,
} from '../api/staff-api';

/** The interview session and task queue endpoints. */
@Injectable({ providedIn: 'root' })
export class SessionsService {
  private readonly api = inject(STAFF_API);

  /** The team's Planned and Live sessions, soonest first. */
  async listTeam(): Promise<ApiResult<TeamSession[]>> {
    const { data, error } = await this.api.GET('/api/staff/sessions');
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  /** A candidate's sessions by number. */
  async listForCandidate(candidateId: string): Promise<ApiResult<CandidateSession[]>> {
    const { data, error } = await this.api.GET('/api/staff/candidates/{candidateId}/sessions', {
      params: { path: { candidateId } },
    });
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  /** Plans a session for the candidate; it gets the candidate's next session number. */
  async create(candidateId: string, scheduledAt: string | null): Promise<ApiResult<CreatedSession>> {
    const { data, error } = await this.api.POST('/api/staff/candidates/{candidateId}/sessions', {
      params: { path: { candidateId } },
      body: { scheduledAt },
    });
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  /** The session with its queue; opening it re-pins Pending tasks to their task's active version. */
  async get(sessionId: string): Promise<ApiResult<InterviewSession>> {
    const { data, error } = await this.api.GET('/api/staff/sessions/{sessionId}', {
      params: { path: { sessionId } },
    });
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  async delete(sessionId: string): Promise<ApiResult<void>> {
    const { error, response } = await this.api.DELETE('/api/staff/sessions/{sessionId}', {
      params: { path: { sessionId } },
    });
    return response.ok ? { ok: true, value: undefined } : apiFailure(error);
  }

  async reschedule(sessionId: string, scheduledAt: string | null): Promise<ApiResult<void>> {
    const { error, response } = await this.api.PUT('/api/staff/sessions/{sessionId}/schedule', {
      params: { path: { sessionId } },
      body: { scheduledAt },
    });
    return response.ok ? { ok: true, value: undefined } : apiFailure(error);
  }

  async addTask(sessionId: string, taskId: string): Promise<ApiResult<void>> {
    const { error, response } = await this.api.POST('/api/staff/sessions/{sessionId}/tasks', {
      params: { path: { sessionId } },
      body: { taskId },
    });
    return response.ok ? { ok: true, value: undefined } : apiFailure(error);
  }

  async removeTask(sessionId: string, sessionTaskId: string): Promise<ApiResult<void>> {
    const { error, response } = await this.api.DELETE(
      '/api/staff/sessions/{sessionId}/tasks/{sessionTaskId}',
      { params: { path: { sessionId, sessionTaskId } } },
    );
    return response.ok ? { ok: true, value: undefined } : apiFailure(error);
  }

  /** Puts the queue in the order of `sessionTaskIds`, which must list every task in it. */
  async reorder(sessionId: string, sessionTaskIds: readonly string[]): Promise<ApiResult<void>> {
    const { error, response } = await this.api.PUT('/api/staff/sessions/{sessionId}/tasks/order', {
      params: { path: { sessionId } },
      body: { sessionTaskIds: [...sessionTaskIds] },
    });
    return response.ok ? { ok: true, value: undefined } : apiFailure(error);
  }
}
