import { Injectable, inject } from '@angular/core';
import { type ApiResult, apiFailure } from '../api/api-result';
import { STAFF_API, type TeamSummary, type UserSummary } from '../api/staff-api';

/**
 * Whose users an action goes through: the admin's user endpoints (`users`, everyone) or a manager's own team
 * (`my-team`, their interviewers).
 */
export type UserScope = 'users' | 'my-team';

/** The team and user management endpoints. */
@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly api = inject(STAFF_API);

  async list(scope: UserScope): Promise<ApiResult<UserSummary[]>> {
    const { data, error } =
      scope === 'users'
        ? await this.api.GET('/api/staff/users')
        : await this.api.GET('/api/staff/my-team/interviewers');
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  async deactivate(scope: UserScope, userId: string): Promise<ApiResult<void>> {
    const params = { path: { userId } };
    const { error, response } =
      scope === 'users'
        ? await this.api.POST('/api/staff/users/{userId}/deactivate', { params })
        : await this.api.POST('/api/staff/my-team/interviewers/{userId}/deactivate', { params });
    return response.ok ? { ok: true, value: undefined } : apiFailure(error);
  }

  async reactivate(scope: UserScope, userId: string): Promise<ApiResult<void>> {
    const params = { path: { userId } };
    const { error, response } =
      scope === 'users'
        ? await this.api.POST('/api/staff/users/{userId}/reactivate', { params })
        : await this.api.POST('/api/staff/my-team/interviewers/{userId}/reactivate', { params });
    return response.ok ? { ok: true, value: undefined } : apiFailure(error);
  }

  async reassign(userId: string, teamId: string): Promise<ApiResult<void>> {
    const { error, response } = await this.api.POST('/api/staff/users/{userId}/reassign', {
      params: { path: { userId } },
      body: { teamId },
    });
    return response.ok ? { ok: true, value: undefined } : apiFailure(error);
  }

  async teams(): Promise<ApiResult<TeamSummary[]>> {
    const { data, error } = await this.api.GET('/api/staff/teams');
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  async renameTeam(teamId: string, name: string): Promise<ApiResult<void>> {
    const { error, response } = await this.api.POST('/api/staff/teams/{teamId}/rename', {
      params: { path: { teamId } },
      body: { name },
    });
    return response.ok ? { ok: true, value: undefined } : apiFailure(error);
  }
}
