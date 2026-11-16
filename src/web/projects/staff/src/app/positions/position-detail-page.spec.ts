import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { fireEvent, render, screen } from '@testing-library/angular';
import { type JobPosition, STAFF_API_FETCH } from '../api/staff-api';
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

  async function renderPage(shown: JobPosition, role: 'Manager' | 'Interviewer' = 'Manager') {
    fake = new FakeFetch().on('GET', path, () => json(200, shown));
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
