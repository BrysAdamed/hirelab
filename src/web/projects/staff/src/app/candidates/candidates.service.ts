import { Injectable, inject } from '@angular/core';
import { type ApiResult, apiFailure } from '../api/api-result';
import {
  type AddedCandidate,
  type Candidate,
  type CandidateDetails,
  type PositionCandidate,
  STAFF_API,
} from '../api/staff-api';

/** The candidate endpoints. */
@Injectable({ providedIn: 'root' })
export class CandidatesService {
  private readonly api = inject(STAFF_API);

  /** A position's candidates by number. */
  async list(positionId: string): Promise<ApiResult<PositionCandidate[]>> {
    const { data, error } = await this.api.GET('/api/staff/positions/{positionId}/candidates', {
      params: { path: { positionId } },
    });
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  async get(candidateId: string): Promise<ApiResult<Candidate>> {
    const { data, error } = await this.api.GET('/api/staff/candidates/{candidateId}', {
      params: { path: { candidateId } },
    });
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  /** Adds a candidate to an open position; they get the position's next number. */
  async add(positionId: string, details: CandidateDetails): Promise<ApiResult<AddedCandidate>> {
    const { data, error } = await this.api.POST('/api/staff/positions/{positionId}/candidates', {
      params: { path: { positionId } },
      body: details,
    });
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  /**
   * Saves an edit based on the `version` the candidate was read with. Fails with `changedMeanwhileCode` when someone
   * else saved them since.
   */
  async edit(
    candidateId: string,
    details: CandidateDetails,
    version: number,
  ): Promise<ApiResult<void>> {
    const { error, response } = await this.api.PUT('/api/staff/candidates/{candidateId}', {
      params: { path: { candidateId } },
      body: { ...details, version },
    });
    return response.ok ? { ok: true, value: undefined } : apiFailure(error);
  }
}
