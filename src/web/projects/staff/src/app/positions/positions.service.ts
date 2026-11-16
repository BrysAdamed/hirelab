import { Injectable, inject } from '@angular/core';
import { type ApiResult, apiFailure } from '../api/api-result';
import {
  type JobPosition,
  type JobPositionDetails,
  type JobPositionState,
  type JobPositionSummary,
  STAFF_API,
} from '../api/staff-api';

/** The job position endpoints. */
@Injectable({ providedIn: 'root' })
export class PositionsService {
  private readonly api = inject(STAFF_API);

  async list(state: JobPositionState): Promise<ApiResult<JobPositionSummary[]>> {
    const { data, error } = await this.api.GET('/api/staff/positions', {
      params: { query: { state } },
    });
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  async get(positionId: string): Promise<ApiResult<JobPosition>> {
    const { data, error } = await this.api.GET('/api/staff/positions/{positionId}', {
      params: { path: { positionId } },
    });
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  /** Creates an open position and returns its id. */
  async create(details: JobPositionDetails): Promise<ApiResult<string>> {
    const { data, error } = await this.api.POST('/api/staff/positions', { body: details });
    return data ? { ok: true, value: data.id } : apiFailure(error);
  }

  /**
   * Saves an edit based on the `version` the position was read with. Fails with `changedMeanwhileCode` when someone
   * else saved it since.
   */
  async edit(
    positionId: string,
    details: JobPositionDetails,
    version: number,
  ): Promise<ApiResult<void>> {
    const { error, response } = await this.api.PUT('/api/staff/positions/{positionId}', {
      params: { path: { positionId } },
      body: { ...details, version },
    });
    return response.ok ? { ok: true, value: undefined } : apiFailure(error);
  }

  /**
   * Closes the position for good and anonymises its candidates, based on the `version` the position was read with.
   * Fails with `changedMeanwhileCode` when someone changed it since, for example by adding a candidate.
   */
  async close(positionId: string, version: number): Promise<ApiResult<void>> {
    const { error, response } = await this.api.POST('/api/staff/positions/{positionId}/close', {
      params: { path: { positionId } },
      body: { version },
    });
    return response.ok ? { ok: true, value: undefined } : apiFailure(error);
  }

  async delete(positionId: string): Promise<ApiResult<void>> {
    const { error, response } = await this.api.DELETE('/api/staff/positions/{positionId}', {
      params: { path: { positionId } },
    });
    return response.ok ? { ok: true, value: undefined } : apiFailure(error);
  }
}
