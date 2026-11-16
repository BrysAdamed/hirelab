import { Injectable, inject } from '@angular/core';
import {
  type Invite,
  type InvitePreview,
  type IssuedInvite,
  STAFF_API,
  type TeamWithoutManager,
} from '../api/staff-api';
import { type ApiResult, apiFailure } from '../api/api-result';
import { AuthService } from '../auth/auth.service';

export type { ApiResult } from '../api/api-result';

/** Who a section invites: the admin invites managers, a manager invites interviewers. */
export type InviteKind = 'managers' | 'interviewers';

export interface InviteeInput {
  email: string;
  displayName: string;
}

export type ManagerTeamInput = { teamId: string } | { newTeamName: string };

/** The invite endpoints: issuing, listing and revoking invites, and redeeming an invite link. */
@Injectable({ providedIn: 'root' })
export class InvitesService {
  private readonly api = inject(STAFF_API);
  private readonly auth = inject(AuthService);

  async list(kind: InviteKind): Promise<ApiResult<Invite[]>> {
    const { data, error } =
      kind === 'managers'
        ? await this.api.GET('/api/staff/invites/managers')
        : await this.api.GET('/api/staff/invites/interviewers');
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  async inviteManager(
    invitee: InviteeInput,
    team: ManagerTeamInput,
  ): Promise<ApiResult<IssuedInvite>> {
    const { data, error } = await this.api.POST('/api/staff/invites/managers', {
      body: {
        ...invitee,
        teamId: 'teamId' in team ? team.teamId : null,
        newTeamName: 'newTeamName' in team ? team.newTeamName : null,
      },
    });
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  async inviteInterviewer(invitee: InviteeInput): Promise<ApiResult<IssuedInvite>> {
    const { data, error } = await this.api.POST('/api/staff/invites/interviewers', {
      body: invitee,
    });
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  async revoke(kind: InviteKind, inviteId: string): Promise<ApiResult<void>> {
    const params = { path: { inviteId } };
    const { error, response } =
      kind === 'managers'
        ? await this.api.POST('/api/staff/invites/managers/{inviteId}/revoke', { params })
        : await this.api.POST('/api/staff/invites/interviewers/{inviteId}/revoke', { params });
    return response.ok ? { ok: true, value: undefined } : apiFailure(error);
  }

  async teamsWithoutManager(): Promise<ApiResult<TeamWithoutManager[]>> {
    const { data, error } = await this.api.GET('/api/staff/invites/managers/teams-without-manager');
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  /** Gets an antiforgery token for the current (usually anonymous) user, which redemption needs. */
  async prepareRedemption(): Promise<void> {
    await this.api.GET('/api/staff/auth/antiforgery');
  }

  async preview(token: string): Promise<ApiResult<InvitePreview>> {
    const { data, error } = await this.api.POST('/api/staff/invites/preview', {
      body: { token },
    });
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  /** Redeems the link; on success the API has signed the new user in. */
  async redeem(token: string, password: string): Promise<ApiResult<void>> {
    const { data, error } = await this.api.POST('/api/staff/invites/redeem', {
      body: { token, password },
    });
    if (!data) {
      return apiFailure(error);
    }
    this.auth.signedIn(data);
    return { ok: true, value: undefined };
  }
}

/** The full invite link for a link path the API returned. */
export function inviteLink(linkPath: string, origin: string): string {
  return new URL(linkPath, origin).toString();
}
