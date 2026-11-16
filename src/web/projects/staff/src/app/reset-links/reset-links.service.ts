import { Injectable, inject } from '@angular/core';
import { type ApiResult, apiFailure } from '../api/api-result';
import { type IssuedResetLink, type ResetLinkPreview, STAFF_API } from '../api/staff-api';

/** Whose reset links a staff user generates: the admin managers', a manager their interviewers'. */
export type ResetLinkKind = 'managers' | 'interviewers';

/** The reset link endpoints: generating a link for a user, and redeeming a link. */
@Injectable({ providedIn: 'root' })
export class ResetLinksService {
  private readonly api = inject(STAFF_API);

  async generate(kind: ResetLinkKind, userId: string): Promise<ApiResult<IssuedResetLink>> {
    const params = { path: { userId } };
    const { data, error } =
      kind === 'managers'
        ? await this.api.POST('/api/staff/reset-links/managers/{userId}', { params })
        : await this.api.POST('/api/staff/reset-links/interviewers/{userId}', { params });
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  /** Gets an antiforgery token for the current (usually anonymous) user, which redemption needs. */
  async prepareRedemption(): Promise<void> {
    await this.api.GET('/api/staff/auth/antiforgery');
  }

  async preview(token: string): Promise<ApiResult<ResetLinkPreview>> {
    const { data, error } = await this.api.POST('/api/staff/reset-links/preview', {
      body: { token },
    });
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  /** Sets the new password. The user is not signed in: they log in with the new password next. */
  async redeem(token: string, password: string): Promise<ApiResult<void>> {
    const { error, response } = await this.api.POST('/api/staff/reset-links/redeem', {
      body: { token, password },
    });
    return response.ok ? { ok: true, value: undefined } : apiFailure(error);
  }
}
