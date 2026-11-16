import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { render, screen, within } from '@testing-library/angular';
import { STAFF_API_FETCH, type TeamSession } from '../api/staff-api';
import { FakeFetch, json } from '../testing/fake-fetch';
import { SessionsPage } from './sessions-page';

describe('SessionsPage', () => {
  const live: TeamSession = {
    id: '0190a5c4-7e3d-7b8a-9f21-000000000081',
    number: 2,
    state: 'Live',
    scheduledAt: '2026-01-06T09:00:00Z',
    taskCount: 3,
    candidateId: '0190a5c4-7e3d-7b8a-9f21-000000000051',
    candidateDisplayName: 'Jane Fakeperson',
  };
  const planned: TeamSession = {
    ...live,
    id: '0190a5c4-7e3d-7b8a-9f21-000000000082',
    number: 1,
    state: 'Planned',
    scheduledAt: null,
    taskCount: 0,
    candidateId: '0190a5c4-7e3d-7b8a-9f21-000000000052',
    candidateDisplayName: 'Candidate #4',
  };

  async function renderPage(listed: TeamSession[]) {
    const fake = new FakeFetch().on('GET', '/api/staff/sessions', () => json(200, listed));
    await render(SessionsPage, {
      providers: [provideRouter([]), { provide: STAFF_API_FETCH, useValue: fake.fetch }],
    });
  }

  afterEach(() => TestBed.resetTestingModule());

  it('lists the sessions in the order the API gives with links to them and their candidates', async () => {
    // Act
    await renderPage([live, planned]);

    // Assert
    const rows = (await screen.findAllByRole('row')).slice(1);
    expect(rows.map((row) => within(row).getAllByRole('link')[0].textContent?.trim())).toEqual([
      'Jane Fakeperson',
      'Candidate #4',
    ]);
    expect(within(rows[0]).getByRole('link', { name: 'Session 2' }).getAttribute('href')).toBe(
      `/sessions/${live.id}`,
    );
    expect(within(rows[0]).getByText('Live')).toBeTruthy();
    expect(within(rows[1]).getByText('Not scheduled')).toBeTruthy();
    expect(within(rows[1]).getByText('Planned')).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Candidate #4' }).getAttribute('href')).toBe(
      `/candidates/${planned.candidateId}`,
    );
  });

  it('says when there are no sessions', async () => {
    // Act
    await renderPage([]);

    // Assert
    expect(await screen.findByText('No planned or live sessions.')).toBeTruthy();
  });
});
