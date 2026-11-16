import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { fireEvent, render, screen } from '@testing-library/angular';
import { type JobPosition, type PositionCandidate, STAFF_API_FETCH } from '../api/staff-api';
import { AuthService } from '../auth/auth.service';
import { FakeFetch, fakeUser, json, noContent } from '../testing/fake-fetch';
import { PositionDetailPage } from './position-detail-page';

describe('PositionDetailPage', () => {
  const position: JobPosition = {
    id: '0190a5c4-7e3d-7b8a-9f21-000000000041',
    title: 'Fake Backend Developer',
    description: null,
    requirements: '- **Three** fake years',
    niceToHaves: '- Imaginary cloud',
    interviewerNotes: null,
    state: 'Open',
    createdAt: '2026-01-05T09:00:00Z',
    version: 7,
  };
  const path = `/api/staff/positions/${position.id}`;

  let fake: FakeFetch;

  const candidates: PositionCandidate[] = [
    {
      id: '0190a5c4-7e3d-7b8a-9f21-000000000051',
      number: 1,
      displayName: 'Jane Fakeperson',
      email: 'jane@example.com',
      phone: null,
      isAnonymised: false,
      createdAt: '2026-01-06T09:00:00Z',
    },
    {
      id: '0190a5c4-7e3d-7b8a-9f21-000000000052',
      number: 2,
      displayName: 'Candidate #2',
      email: null,
      phone: null,
      isAnonymised: true,
      createdAt: '2026-01-07T09:00:00Z',
    },
  ];

  async function renderPage(
    shown: JobPosition,
    role: 'Manager' | 'Interviewer' = 'Manager',
    listed: PositionCandidate[] = [],
  ) {
    fake = new FakeFetch()
      .on('GET', path, () => json(200, shown))
      .on('GET', `${path}/candidates`, () => json(200, listed));
    await render(PositionDetailPage, {
      inputs: { positionId: shown.id },
      providers: [provideRouter([]), { provide: STAFF_API_FETCH, useValue: fake.fetch }],
    });
    TestBed.inject(AuthService).signedIn(fakeUser(role));
  }

  afterEach(() => TestBed.resetTestingModule());

  it('shows the filled-in sections as Markdown and a place for candidates', async () => {
    // Act
    await renderPage(position);

    // Assert
    expect(await screen.findByRole('heading', { name: 'Fake Backend Developer' })).toBeTruthy();
    expect(screen.getByRole('heading', { name: 'Requirements' })).toBeTruthy();
    expect(screen.getByText('Three').tagName).toBe('STRONG');
    expect(screen.getByRole('heading', { name: 'Nice-to-haves' })).toBeTruthy();
    expect(screen.queryByRole('heading', { name: 'Description' })).toBeNull();
    expect(screen.getByRole('heading', { name: 'Candidates' })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Edit' })).toBeTruthy();
    expect(await screen.findByText('No candidates yet.')).toBeTruthy();
  });

  it('lists the candidates by number with links to their profiles', async () => {
    // Act
    await renderPage(position, 'Interviewer', candidates);

    // Assert
    const jane = await screen.findByRole('link', { name: 'Jane Fakeperson' });
    expect(jane.getAttribute('href')).toBe(`/candidates/${candidates[0].id}`);
    expect(screen.getByRole('link', { name: 'Candidate #2' })).toBeTruthy();
    expect(screen.getByText('jane@example.com')).toBeTruthy();
    expect(
      screen.getByRole('link', { name: 'Add candidate' }).getAttribute('href'),
    ).toBe(`/positions/${position.id}/candidates/new`);
  });

  it('offers no new candidates on a closed position', async () => {
    // Act
    await renderPage({ ...position, state: 'Closed' }, 'Manager', candidates);

    // Assert
    await screen.findByRole('link', { name: 'Jane Fakeperson' });
    expect(screen.queryByRole('link', { name: 'Add candidate' })).toBeNull();
  });

  it('offers no changes to interviewers or on a closed position', async () => {
    // Act
    await renderPage({ ...position, state: 'Closed' });

    // Assert
    expect(await screen.findByText('This position is closed and read-only.')).toBeTruthy();
    expect(screen.queryByRole('link', { name: 'Edit' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Delete' })).toBeNull();
  });

  it('hides the manager actions from interviewers', async () => {
    // Act
    await renderPage(position, 'Interviewer');

    // Assert
    await screen.findByRole('heading', { name: 'Fake Backend Developer' });
    expect(screen.queryByRole('link', { name: 'Edit' })).toBeNull();
  });

  it('deletes the position after confirmation and goes back to the list', async () => {
    // Arrange
    await renderPage(position);
    fake.on('DELETE', path, noContent);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Delete' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Delete position' }));

    // Assert
    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith('/positions'));
    expect(fake.requests.some((request) => request.method === 'DELETE')).toBe(true);
  });
});
