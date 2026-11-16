import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import { type Candidate, STAFF_API_FETCH } from '../api/staff-api';
import { FakeFetch, json } from '../testing/fake-fetch';
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
    createdAt: '2026-01-06T09:00:00Z',
    version: 5,
  };

  async function renderPage(shown: Candidate) {
    const fake = new FakeFetch().on('GET', `/api/staff/candidates/${shown.id}`, () =>
      json(200, shown),
    );
    await render(CandidateProfilePage, {
      inputs: { candidateId: shown.id },
      providers: [provideRouter([]), { provide: STAFF_API_FETCH, useValue: fake.fetch }],
    });
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

  it('shows an anonymised candidate by number without an edit link', async () => {
    // Act
    await renderPage({
      ...candidate,
      displayName: 'Candidate #3',
      fullName: null,
      email: null,
      phone: null,
      links: [],
      isAnonymised: true,
    });

    // Assert
    expect(await screen.findByRole('heading', { name: 'Candidate #3' })).toBeTruthy();
    expect(screen.getByText(/has been anonymised/)).toBeTruthy();
    expect(screen.queryByRole('link', { name: 'Edit' })).toBeNull();
  });

  it('offers no edit when the position is closed', async () => {
    // Act
    await renderPage({ ...candidate, positionState: 'Closed' });

    // Assert
    expect(await screen.findByText(/position is closed/)).toBeTruthy();
    expect(screen.queryByRole('link', { name: 'Edit' })).toBeNull();
  });
});
