import { TestBed } from '@angular/core/testing';
import { fireEvent, render, screen, within } from '@testing-library/angular';
import { STAFF_API_FETCH, type TeamSummary, type UserSummary } from '../api/staff-api';
import { FakeFetch, json, noContent } from '../testing/fake-fetch';
import { TeamsPage } from './teams-page';

describe('TeamsPage', () => {
  let fake: FakeFetch;

  const alpha = { id: '0190a5c4-7e3d-7b8a-9f21-0000000000a1', name: 'Fake Team Alpha' };
  const beta = { id: '0190a5c4-7e3d-7b8a-9f21-0000000000b1', name: 'Fake Team Beta' };

  const manager: UserSummary = {
    id: '0190a5c4-7e3d-7b8a-9f21-000000000011',
    email: 'mona@example.com',
    displayName: 'Mona Manager',
    role: 'Manager',
    team: alpha,
    isActive: true,
  };
  const interviewer: UserSummary = {
    id: '0190a5c4-7e3d-7b8a-9f21-000000000012',
    email: 'ivo@example.com',
    displayName: 'Ivo Interviewer',
    role: 'Interviewer',
    team: alpha,
    isActive: true,
  };
  const formerManager: UserSummary = {
    id: '0190a5c4-7e3d-7b8a-9f21-000000000013',
    email: 'fred@example.com',
    displayName: 'Fred Former',
    role: 'Manager',
    team: beta,
    isActive: false,
  };

  const teams: TeamSummary[] = [
    { ...alpha, manager, members: [manager, interviewer] },
    { ...beta, manager: null, members: [formerManager] },
  ];

  function posted(path: string): Request | undefined {
    return fake.requests.find(
      (request) => request.method === 'POST' && new URL(request.url).pathname === path,
    );
  }

  async function renderPage(): Promise<void> {
    await render(TeamsPage, { providers: [{ provide: STAFF_API_FETCH, useValue: fake.fetch }] });
  }

  async function teamSection(name: string): Promise<HTMLElement> {
    return (await screen.findByRole('heading', { name })).closest('section')!;
  }

  beforeEach(() => {
    fake = new FakeFetch()
      .on('GET', '/api/staff/teams', () => json(200, teams))
      .on('GET', '/api/staff/invites/managers', () => json(200, []))
      .on('GET', '/api/staff/invites/managers/teams-without-manager', () => json(200, [beta]));
  });

  afterEach(() => TestBed.resetTestingModule());

  it('shows each team with its manager, or No manager', async () => {
    // Act
    await renderPage();

    // Assert
    const alphaSection = await teamSection('Fake Team Alpha');
    expect(within(alphaSection).getByText('Manager: Mona Manager')).toBeTruthy();
    expect(within(alphaSection).getByText('ivo@example.com')).toBeTruthy();
    const betaSection = await teamSection('Fake Team Beta');
    expect(within(betaSection).getByText('No manager')).toBeTruthy();
    const formerRow = within(betaSection).getByText('fred@example.com').closest('tr')!;
    expect(within(formerRow).getByText('Deactivated')).toBeTruthy();
    expect(within(formerRow).getByRole('button', { name: 'Reactivate Fred Former' })).toBeTruthy();
  });

  it('offers reassignment for interviewers only', async () => {
    // Act
    await renderPage();

    // Assert
    expect(await screen.findByRole('button', { name: 'Reassign Ivo Interviewer' })).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Reassign Mona Manager' })).toBeNull();
  });

  it('reassigns an interviewer to the chosen team', async () => {
    // Arrange
    fake.on('POST', `/api/staff/users/${interviewer.id}/reassign`, noContent);
    await renderPage();

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Reassign Ivo Interviewer' }));
    fireEvent.change(await screen.findByLabelText('New team'), { target: { value: beta.id } });
    fireEvent.click(screen.getByRole('button', { name: 'Move to team' }));

    // Assert
    await vi.waitFor(() =>
      expect(posted(`/api/staff/users/${interviewer.id}/reassign`)).toBeDefined(),
    );
    expect(await posted(`/api/staff/users/${interviewer.id}/reassign`)!.json()).toEqual({
      teamId: beta.id,
    });
  });

  it('deactivates a manager after confirmation', async () => {
    // Arrange
    fake.on('POST', `/api/staff/users/${manager.id}/deactivate`, noContent);
    await renderPage();

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Deactivate Mona Manager' }));
    const dialog = await screen.findByRole('dialog');
    fireEvent.click(within(dialog).getByRole('button', { name: 'Deactivate user' }));

    // Assert
    expect(dialog.textContent).toContain('Their pending invites are revoked');
    await vi.waitFor(() =>
      expect(posted(`/api/staff/users/${manager.id}/deactivate`)).toBeDefined(),
    );
  });

  it('shows the API message when a reactivation is refused', async () => {
    // Arrange
    fake.on('POST', `/api/staff/users/${formerManager.id}/reactivate`, () =>
      json(409, {
        status: 409,
        detail: "This manager's team already has another active manager.",
      }),
    );
    await renderPage();

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Reactivate Fred Former' }));

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain(
      'already has another active manager',
    );
  });

  it('renames a team', async () => {
    // Arrange
    fake.on('POST', `/api/staff/teams/${alpha.id}/rename`, noContent);
    await renderPage();

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Rename Fake Team Alpha' }));
    const dialog = await screen.findByRole('dialog');
    fireEvent.input(within(dialog).getByLabelText('Team name'), {
      target: { value: 'Fake Team Omega' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Save name' }));

    // Assert
    await vi.waitFor(() => expect(posted(`/api/staff/teams/${alpha.id}/rename`)).toBeDefined());
    expect(await posted(`/api/staff/teams/${alpha.id}/rename`)!.json()).toEqual({
      name: 'Fake Team Omega',
    });
  });
});
