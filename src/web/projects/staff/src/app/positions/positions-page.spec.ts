import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { render, screen, within } from '@testing-library/angular';
import { type JobPositionSummary, STAFF_API_FETCH } from '../api/staff-api';
import { AuthService } from '../auth/auth.service';
import { FakeFetch, fakeUser, json } from '../testing/fake-fetch';
import { PositionsPage } from './positions-page';

describe('PositionsPage', () => {
  const open: JobPositionSummary = {
    id: '0190a5c4-7e3d-7b8a-9f21-000000000041',
    title: 'Fake Backend Developer',
    state: 'Open',
    createdAt: '2026-01-05T09:00:00Z',
  };
  const closed: JobPositionSummary = {
    ...open,
    id: '0190a5c4-7e3d-7b8a-9f21-000000000042',
    title: 'Fake Closed Role',
    state: 'Closed',
  };

  let fake: FakeFetch;

  async function renderPage(state?: string, role: 'Manager' | 'Interviewer' = 'Manager') {
    fake = new FakeFetch().on('GET', '/api/staff/positions', (request) =>
      json(200, new URL(request.url).searchParams.get('state') === 'Closed' ? [closed] : [open]),
    );
    await render(PositionsPage, {
      inputs: { state },
      providers: [provideRouter([]), { provide: STAFF_API_FETCH, useValue: fake.fetch }],
    });
    TestBed.inject(AuthService).signedIn(fakeUser(role));
  }

  afterEach(() => TestBed.resetTestingModule());

  it('lists open positions by default with a link to each', async () => {
    // Act
    await renderPage();

    // Assert
    const link = await screen.findByRole('link', { name: 'Fake Backend Developer' });
    expect(link.getAttribute('href')).toBe(`/positions/${open.id}`);
    expect(within(link.closest('tr')!).getByText('Open')).toBeTruthy();
    const filter = screen.getByRole('group', { name: 'Filter by state' });
    expect(within(filter).getByRole('link', { name: 'Open' }).getAttribute('aria-current')).toBe(
      'page',
    );
  });

  it('lists closed positions when the filter says so', async () => {
    // Act
    await renderPage('closed');

    // Assert
    expect(await screen.findByRole('link', { name: 'Fake Closed Role' })).toBeTruthy();
    expect(new URL(fake.requests[0].url).searchParams.get('state')).toBe('Closed');
  });

  it('offers a new position to managers only', async () => {
    // Act
    await renderPage(undefined, 'Interviewer');

    // Assert
    await screen.findByRole('link', { name: 'Fake Backend Developer' });
    expect(screen.queryByRole('link', { name: 'New position' })).toBeNull();
  });
});
