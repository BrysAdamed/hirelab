import { TestBed } from '@angular/core/testing';
import { fireEvent, render, screen, within } from '@testing-library/angular';
import { STAFF_API_FETCH, type UserSummary } from '../api/staff-api';
import { FakeFetch, json, noContent } from '../testing/fake-fetch';
import { MyTeamPage } from './my-team-page';

describe('MyTeamPage', () => {
  let fake: FakeFetch;

  const team = { id: '0190a5c4-7e3d-7b8a-9f21-000000000001', name: 'Fake Team' };
  const active: UserSummary = {
    id: '0190a5c4-7e3d-7b8a-9f21-000000000021',
    email: 'ada@example.com',
    displayName: 'Ada Active',
    role: 'Interviewer',
    team,
    isActive: true,
  };
  const inactive: UserSummary = {
    ...active,
    id: '0190a5c4-7e3d-7b8a-9f21-000000000022',
    email: 'ike@example.com',
    displayName: 'Ike Inactive',
    isActive: false,
  };

  function posted(path: string): Request | undefined {
    return fake.requests.find(
      (request) => request.method === 'POST' && new URL(request.url).pathname === path,
    );
  }

  beforeEach(async () => {
    fake = new FakeFetch()
      .on('GET', '/api/staff/my-team/interviewers', () => json(200, [active, inactive]))
      .on('GET', '/api/staff/invites/interviewers', () => json(200, []));
    await render(MyTeamPage, { providers: [{ provide: STAFF_API_FETCH, useValue: fake.fetch }] });
  });

  afterEach(() => TestBed.resetTestingModule());

  it('lists the team’s interviewers with their state', async () => {
    // Assert
    const activeRow = (await screen.findByText('ada@example.com')).closest('tr')!;
    expect(within(activeRow).getByText('Active')).toBeTruthy();
    const inactiveRow = screen.getByText('ike@example.com').closest('tr')!;
    expect(within(inactiveRow).getByText('Deactivated')).toBeTruthy();
    expect(screen.queryByRole('button', { name: /^Reassign/ })).toBeNull();
  });

  it('deactivates an interviewer through the team endpoint after confirmation', async () => {
    // Arrange
    fake.on('POST', `/api/staff/my-team/interviewers/${active.id}/deactivate`, noContent);

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Deactivate Ada Active' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Deactivate user' }));

    // Assert
    await vi.waitFor(() =>
      expect(posted(`/api/staff/my-team/interviewers/${active.id}/deactivate`)).toBeDefined(),
    );
  });

  it('reactivates an interviewer through the team endpoint', async () => {
    // Arrange
    fake.on('POST', `/api/staff/my-team/interviewers/${inactive.id}/reactivate`, noContent);

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Reactivate Ike Inactive' }));

    // Assert
    await vi.waitFor(() =>
      expect(posted(`/api/staff/my-team/interviewers/${inactive.id}/reactivate`)).toBeDefined(),
    );
  });
});
