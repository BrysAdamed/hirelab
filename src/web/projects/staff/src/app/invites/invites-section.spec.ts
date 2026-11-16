import { TestBed } from '@angular/core/testing';
import { fireEvent, render, screen, within } from '@testing-library/angular';
import { type Invite, STAFF_API_FETCH } from '../api/staff-api';
import { FakeFetch, json, noContent } from '../testing/fake-fetch';
import { InvitesSection } from './invites-section';
import type { InviteKind } from './invites.service';

describe('InvitesSection', () => {
  let fake: FakeFetch;

  const pendingInvite: Invite = {
    id: '0190a5c4-7e3d-7b8a-9f21-00000000000a',
    email: 'ivan@example.com',
    displayName: 'Ivan Interviewer',
    role: 'Interviewer',
    team: { id: '0190a5c4-7e3d-7b8a-9f21-000000000001', name: 'Fake Team', isNew: false },
    expiresAt: '2026-01-08T09:00:00+00:00',
    state: 'Pending',
  };

  async function renderSection(kind: InviteKind): Promise<void> {
    await render(InvitesSection, {
      inputs: { kind },
      providers: [{ provide: STAFF_API_FETCH, useValue: fake.fetch }],
    });
  }

  function fillInvitee(email: string, displayName: string): void {
    fireEvent.input(screen.getByLabelText('Email'), { target: { value: email } });
    fireEvent.input(screen.getByLabelText('Display name'), { target: { value: displayName } });
  }

  function posted(path: string): Request | undefined {
    return fake.requests.find(
      (request) => request.method === 'POST' && new URL(request.url).pathname === path,
    );
  }

  beforeEach(() => {
    fake = new FakeFetch()
      .on('GET', '/api/staff/invites/interviewers', () => json(200, [pendingInvite]))
      .on('GET', '/api/staff/invites/managers', () => json(200, []))
      .on('GET', '/api/staff/invites/managers/teams-without-manager', () =>
        json(200, [{ id: '0190a5c4-7e3d-7b8a-9f21-000000000002', name: 'Fake Team Orphaned' }]),
      );
  });

  afterEach(() => TestBed.resetTestingModule());

  it('lists the invites with their state', async () => {
    // Act
    await renderSection('interviewers');

    // Assert
    const row = (await screen.findByText('ivan@example.com')).closest('tr')!;
    expect(within(row).getByText('Pending')).toBeTruthy();
    expect(within(row).getByText('Fake Team')).toBeTruthy();
  });

  it('invites a manager into a new team and shows the link once', async () => {
    // Arrange
    fake.on('POST', '/api/staff/invites/managers', () =>
      json(200, {
        invite: { ...pendingInvite, email: 'nora@example.com', role: 'Manager' },
        linkPath: '/invite#fake-token',
      }),
    );
    await renderSection('managers');
    await screen.findByRole('option', { name: 'Fake Team Orphaned (no manager)' });

    // Act
    fillInvitee('nora@example.com', 'Nora Newmanager');
    fireEvent.input(screen.getByLabelText('Team name'), { target: { value: 'Fake Team Beta' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create invite' }));

    // Assert
    const link = (await screen.findByLabelText('Invite link')) as HTMLInputElement;
    expect(link.value).toBe(`${document.location.origin}/invite#fake-token`);
    expect(await posted('/api/staff/invites/managers')?.json()).toEqual({
      email: 'nora@example.com',
      displayName: 'Nora Newmanager',
      teamId: null,
      newTeamName: 'Fake Team Beta',
    });
  });

  it('hands over an existing team without asking for a team name', async () => {
    // Arrange
    fake.on('POST', '/api/staff/invites/managers', () =>
      json(200, { invite: pendingInvite, linkPath: '/invite#fake-token' }),
    );
    await renderSection('managers');
    await screen.findByRole('option', { name: 'Fake Team Orphaned (no manager)' });

    // Act
    fillInvitee('hank@example.com', 'Hank Handover');
    fireEvent.change(screen.getByLabelText('Team'), {
      target: { value: '0190a5c4-7e3d-7b8a-9f21-000000000002' },
    });
    const teamNameShown = screen.queryByLabelText('Team name') !== null;
    fireEvent.click(screen.getByRole('button', { name: 'Create invite' }));

    // Assert
    await screen.findByLabelText('Invite link');
    expect(teamNameShown).toBe(false);
    expect(await posted('/api/staff/invites/managers')?.json()).toEqual({
      email: 'hank@example.com',
      displayName: 'Hank Handover',
      teamId: '0190a5c4-7e3d-7b8a-9f21-000000000002',
      newTeamName: null,
    });
  });

  it('shows the API message when the invite is refused', async () => {
    // Arrange
    fake.on('POST', '/api/staff/invites/interviewers', () =>
      json(409, { status: 409, detail: 'A user with this email already exists.' }),
    );
    await renderSection('interviewers');

    // Act
    fillInvitee('taken@example.com', 'Taken User');
    fireEvent.click(screen.getByRole('button', { name: 'Create invite' }));

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain(
      'A user with this email already exists.',
    );
    expect(screen.queryByLabelText('Invite link')).toBeNull();
  });

  it('revokes a pending invite after confirmation', async () => {
    // Arrange
    fake.on('POST', `/api/staff/invites/interviewers/${pendingInvite.id}/revoke`, noContent);
    await renderSection('interviewers');

    // Act
    fireEvent.click(
      await screen.findByRole('button', { name: 'Revoke the invite for ivan@example.com' }),
    );
    fireEvent.click(await screen.findByRole('button', { name: 'Revoke invite' }));

    // Assert
    await vi.waitFor(() =>
      expect(posted(`/api/staff/invites/interviewers/${pendingInvite.id}/revoke`)).toBeDefined(),
    );
  });
});
