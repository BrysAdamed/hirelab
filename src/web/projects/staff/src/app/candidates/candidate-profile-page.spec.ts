import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { fireEvent, render, screen } from '@testing-library/angular';
import { type Candidate, type CandidateSession, STAFF_API_FETCH } from '../api/staff-api';
import { AuthService } from '../auth/auth.service';
import { FakeFetch, fakeUser, json, noContent } from '../testing/fake-fetch';
import { CandidateProfilePage } from './candidate-profile-page';

describe('CandidateProfilePage', () => {
  const candidate: Candidate = {
    id: '0190a5c4-7e3d-7b8a-9f21-000000000051',
    positionId: '0190a5c4-7e3d-7b8a-9f21-000000000041',
    positionTitle: 'Fake Backend Developer',
    positionState: 'Open',
    number: 3,
    displayName: 'Jane Fakeperson',
    fullName: 'Jane Fakeperson',
    email: 'jane@example.com',
    phone: '+00 123 456 789',
    links: ['https://profiles.example.com/jane'],
    isAnonymised: false,
    anonymisedAt: null,
    anonymisedById: null,
    anonymisedByName: null,
    createdAt: '2026-01-06T09:00:00Z',
    version: 5,
  };

  const anonymised: Candidate = {
    ...candidate,
    displayName: 'Candidate #3',
    fullName: null,
    email: null,
    phone: null,
    links: [],
    isAnonymised: true,
    anonymisedAt: '2026-01-08T09:00:00Z',
    anonymisedById: '0190a5c4-7e3d-7b8a-9f21-3c4d5e6f7a8b',
    anonymisedByName: 'Fake Manager',
    version: 6,
  };

  let fake: FakeFetch;

  async function renderPage(
    shown: Candidate,
    role: 'Manager' | 'Interviewer' = 'Manager',
    sessions: CandidateSession[] = [],
  ) {
    fake = new FakeFetch()
      .on('GET', `/api/staff/candidates/${shown.id}`, () => json(200, shown))
      .on('GET', `/api/staff/candidates/${shown.id}/attachments`, () => json(200, []))
      .on('GET', `/api/staff/candidates/${shown.id}/sessions`, () => json(200, sessions));
    await render(CandidateProfilePage, {
      inputs: { candidateId: shown.id },
      providers: [provideRouter([]), { provide: STAFF_API_FETCH, useValue: fake.fetch }],
    });
    TestBed.inject(AuthService).signedIn(fakeUser(role));
  }

  afterEach(() => TestBed.resetTestingModule());

  it('shows the contact details, the position and an edit link', async () => {
    // Act
    await renderPage(candidate);

    // Assert
    expect(await screen.findByRole('heading', { name: 'Jane Fakeperson' })).toBeTruthy();
    expect(screen.getByText(/Candidate #3/)).toBeTruthy();
    expect(screen.getByRole('link', { name: 'jane@example.com' }).getAttribute('href')).toBe(
      'mailto:jane@example.com',
    );
    expect(screen.getByRole('link', { name: '+00 123 456 789' })).toBeTruthy();
    const profile = screen.getByRole('link', { name: /profiles\.example\.com\/jane/ });
    expect(profile.getAttribute('href')).toBe('https://profiles.example.com/jane');
    expect(profile.getAttribute('rel')).toBe('noopener noreferrer');
    expect(screen.getByRole('link', { name: 'Fake Backend Developer' }).getAttribute('href')).toBe(
      `/positions/${candidate.positionId}`,
    );
    expect(screen.getByRole('link', { name: 'Edit' }).getAttribute('href')).toBe(
      `/candidates/${candidate.id}/edit`,
    );
  });

  it('shows an anonymised candidate by number with who anonymised them and no actions', async () => {
    // Act
    await renderPage(anonymised);

    // Assert
    expect(await screen.findByRole('heading', { name: 'Candidate #3' })).toBeTruthy();
    expect(screen.getByText(/Anonymised on Jan 8, 2026 by Fake Manager/)).toBeTruthy();
    expect(screen.queryByText(/Candidate #3 ·/)).toBeNull();
    expect(screen.queryByRole('heading', { name: 'Contact details' })).toBeNull();
    expect(screen.queryByRole('heading', { name: 'Attachments' })).toBeNull();
    expect(screen.queryByRole('link', { name: 'Edit' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Anonymise' })).toBeNull();
  });

  it('anonymises the candidate after confirmation and shows them by number', async () => {
    // Arrange
    await renderPage(candidate);
    const path = `/api/staff/candidates/${candidate.id}`;
    fake.on('POST', `${path}/anonymise`, noContent);
    fake.on('GET', path, () => json(200, anonymised));

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Anonymise' }));
    expect(await screen.findByText(/attachments are deleted/)).toBeTruthy();
    fireEvent.click(screen.getByRole('button', { name: 'Anonymise candidate' }));

    // Assert
    expect(await screen.findByRole('heading', { name: 'Candidate #3' })).toBeTruthy();
    const request = fake.requests.find((sent) => sent.method === 'POST');
    expect(await request?.json()).toEqual({ version: 5 });
    expect(screen.queryByRole('heading', { name: 'Attachments' })).toBeNull();
  });

  it('offers anonymisation to managers only', async () => {
    // Act
    await renderPage(candidate, 'Interviewer');

    // Assert
    expect(await screen.findByRole('link', { name: 'Edit' })).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Anonymise' })).toBeNull();
  });

  it('offers no edit when the position is closed', async () => {
    // Act
    await renderPage({ ...candidate, positionState: 'Closed' });

    // Assert
    expect(await screen.findByText(/position is closed/)).toBeTruthy();
    expect(screen.queryByRole('link', { name: 'Edit' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Anonymise' })).toBeNull();
  });

  it('lists the sessions with links and plans a new one', async () => {
    // Arrange
    const planned: CandidateSession = {
      id: '0190a5c4-7e3d-7b8a-9f21-000000000071',
      number: 1,
      state: 'Planned',
      scheduledAt: null,
      taskCount: 2,
      createdAt: '2026-01-06T09:00:00Z',
    };
    await renderPage(candidate, 'Interviewer', [planned]);
    const created = { id: '0190a5c4-7e3d-7b8a-9f21-000000000072', number: 2 };
    fake.on('POST', `/api/staff/candidates/${candidate.id}/sessions`, () => json(201, created));
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    // Act
    const link = await screen.findByRole('link', { name: 'Session 1' });
    fireEvent.click(screen.getByRole('button', { name: 'Plan session' }));

    // Assert
    expect(link.getAttribute('href')).toBe(`/sessions/${planned.id}`);
    expect(screen.getByText(/Not scheduled · 2 tasks/)).toBeTruthy();
    expect(screen.getByText('Planned')).toBeTruthy();
    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith(['/sessions', created.id]));
  });

  it('shows the sessions of an anonymised candidate but offers no new one', async () => {
    // Act
    await renderPage(anonymised);

    // Assert
    expect(await screen.findByRole('heading', { name: 'Interview sessions' })).toBeTruthy();
    expect(await screen.findByText('No sessions yet.')).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Plan session' })).toBeNull();
  });
});
