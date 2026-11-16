import {
  ChangeDetectionStrategy,
  Component,
  DOCUMENT,
  computed,
  inject,
  input,
  signal,
  type OnInit,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert, lucideLoaderCircle, lucideMailPlus } from '@ng-icons/lucide';
import { CopyButton } from '@hirelab/ui/copy';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import { HlmInput } from '@hirelab/ui/helm/input';
import { HlmLabel } from '@hirelab/ui/helm/label';
import type { Invite, TeamWithoutManager } from '../api/staff-api';
import { InviteList } from './invite-list';
import {
  type ApiResult,
  type InviteKind,
  type ManagerTeamInput,
  InvitesService,
  inviteLink,
} from './invites.service';

/** The value of the team select that means "create a new team". */
const newTeam = '';

const copy: Record<InviteKind, { title: string; description: string; caption: string }> = {
  managers: {
    title: 'Invite a manager',
    description:
      'For a new team, or to hand over a team that has no active manager. The link works once, for 72 hours.',
    caption: 'Manager invites',
  },
  interviewers: {
    title: 'Invite an interviewer',
    description: 'They join your team. The link works once, for 72 hours.',
    caption: 'Interviewer invites',
  },
};

/**
 * Issues invites and lists them. The admin's section (`managers`) also picks the team: a new one, or an existing
 * team without an active manager. A new link is shown once, to be copied and sent by hand.
 */
@Component({
  selector: 'hl-invites-section',
  imports: [
    ReactiveFormsModule,
    NgIcon,
    CopyButton,
    HlmButton,
    HlmCardImports,
    HlmInput,
    HlmLabel,
    InviteList,
  ],
  providers: [provideIcons({ lucideCircleAlert, lucideLoaderCircle, lucideMailPlus })],
  templateUrl: './invites-section.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-col gap-3' },
})
export class InvitesSection implements OnInit {
  private readonly invitesService = inject(InvitesService);
  private readonly origin = inject(DOCUMENT).location.origin;

  readonly kind = input.required<InviteKind>();

  protected readonly newTeam = newTeam;
  protected readonly copy = computed(() => copy[this.kind()]);
  protected readonly idPrefix = computed(() => `invite-${this.kind()}`);

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    displayName: ['', Validators.required],
    teamId: [newTeam],
    newTeamName: [''],
  });
  private readonly teamChoice = toSignal(this.form.controls.teamId.valueChanges, {
    initialValue: newTeam,
  });
  protected readonly isNewTeam = computed(() => this.teamChoice() === newTeam);

  protected readonly invites = signal<Invite[]>([]);
  protected readonly teams = signal<TeamWithoutManager[]>([]);
  protected readonly issuedLink = signal<{ email: string; url: string } | null>(null);
  protected readonly pending = signal(false);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    void this.refresh();
  }

  protected async submit(): Promise<void> {
    if (this.pending()) {
      return;
    }
    const validation = this.validationMessage();
    if (validation) {
      this.error.set(validation);
      return;
    }

    this.pending.set(true);
    this.error.set(null);
    this.issuedLink.set(null);
    const { email, displayName, teamId, newTeamName } = this.form.getRawValue();
    const invitee = { email, displayName };
    const team: ManagerTeamInput = teamId === newTeam ? { newTeamName } : { teamId };
    const result =
      this.kind() === 'managers'
        ? await this.invitesService.inviteManager(invitee, team)
        : await this.invitesService.inviteInterviewer(invitee);
    this.pending.set(false);

    if (!result.ok) {
      this.error.set(result.message);
      return;
    }

    this.issuedLink.set({
      email: result.value.invite.email,
      url: inviteLink(result.value.linkPath, this.origin),
    });
    this.form.reset();
    await this.refresh();
  }

  protected async revoke(invite: Invite): Promise<void> {
    this.error.set(null);
    const result = await this.invitesService.revoke(this.kind(), invite.id);
    if (!result.ok) {
      this.error.set(result.message);
    }
    await this.refresh();
  }

  private validationMessage(): string | null {
    const { email, displayName, newTeamName } = this.form.controls;
    if (email.invalid || displayName.invalid) {
      return 'Enter the invitee’s email and display name.';
    }
    if (this.kind() === 'managers' && this.isNewTeam() && !newTeamName.value.trim()) {
      return 'Enter a name for the new team, or choose an existing team.';
    }
    return null;
  }

  private async refresh(): Promise<void> {
    const [invites, teams] = await Promise.all([
      this.invitesService.list(this.kind()),
      this.kind() === 'managers'
        ? this.invitesService.teamsWithoutManager()
        : Promise.resolve<ApiResult<TeamWithoutManager[]>>({ ok: true, value: [] }),
    ]);
    if (invites.ok) {
      this.invites.set(invites.value);
    }
    if (teams.ok) {
      this.teams.set(teams.value);
    }
    if (!invites.ok || !teams.ok) {
      this.error.set(invites.ok ? (teams.ok ? null : teams.message) : invites.message);
    }
  }
}
