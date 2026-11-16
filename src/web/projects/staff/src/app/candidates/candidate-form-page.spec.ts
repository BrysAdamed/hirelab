import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { fireEvent, render, screen } from '@testing-library/angular';
import { type Candidate, STAFF_API_FETCH } from '../api/staff-api';
import { FakeFetch, json, noContent } from '../testing/fake-fetch';
import { CandidateFormPage, isProfileLink } from './candidate-form-page';

describe('CandidateFormPage', () => {
  const positionId = '0190a5c4-7e3d-7b8a-9f21-000000000041';
  const candidate: Candidate = {
    id: '0190a5c4-7e3d-7b8a-9f21-000000000051',
    positionId,
    positionTitle: 'Fake Backend Developer',
    positionState: 'Open',
    number: 1,
    displayName: 'Jane Fakeperson',
    fullName: 'Jane Fakeperson',
    email: 'jane@example.com',
    phone: null,
    links: ['https://profiles.example.com/jane'],
    isAnonymised: false,
    createdAt: '2026-01-06T09:00:00Z',
    version: 5,
  };
  const path = `/api/staff/candidates/${candidate.id}`;

  let fake: FakeFetch;
  let navigate: ReturnType<typeof vi.spyOn>;

  async function renderPage(inputs: { positionId?: string; candidateId?: string }) {
    await render(CandidateFormPage, {
      inputs,
      providers: [provideRouter([]), { provide: STAFF_API_FETCH, useValue: fake.fetch }],
    });
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  }

  function type(label: string | RegExp, value: string): void {
    fireEvent.input(screen.getByLabelText(label), { target: { value } });
  }

  async function sentBody(method: string): Promise<Record<string, unknown>> {
    const request = fake.requests.find((sent) => sent.method === method)!;
    return (await request.clone().json()) as Record<string, unknown>;
  }

  async function loaded(): Promise<void> {
    await vi.waitFor(() =>
      expect((screen.getByLabelText('Full name') as HTMLInputElement).value).toBe(
        candidate.fullName,
      ),
    );
  }

  beforeEach(() => {
    fake = new FakeFetch().on('GET', path, () => json(200, candidate));
  });

  afterEach(() => TestBed.resetTestingModule());

  it('adds a candidate to the position and opens their profile', async () => {
    // Arrange
    fake.on('POST', `/api/staff/positions/${positionId}/candidates`, () =>
      json(201, { id: candidate.id, number: 1 }),
    );
    await renderPage({ positionId });

    // Act
    type('Full name', '  John Fakeman ');
    type(/Email/, 'john@example.com');
    type('Profile link 2', ' https://code.example.org/john ');
    fireEvent.click(screen.getByRole('button', { name: 'Add candidate' }));

    // Assert
    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith(['/candidates', candidate.id]));
    expect(await sentBody('POST')).toEqual({
      fullName: 'John Fakeman',
      email: 'john@example.com',
      phone: null,
      links: ['https://code.example.org/john'],
    });
  });

  it('asks for a full profile link before sending anything', async () => {
    // Arrange
    await renderPage({ positionId });

    // Act
    type('Full name', 'John Fakeman');
    type('Profile link 1', 'profiles.example.com/john');
    fireEvent.click(screen.getByRole('button', { name: 'Add candidate' }));

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('full http or https address');
    expect(fake.requests).toHaveLength(0);
  });

  it('shows why the API refused the candidate', async () => {
    // Arrange
    fake.on('POST', `/api/staff/positions/${positionId}/candidates`, () =>
      json(409, { status: 409, detail: 'This job position is closed.', code: 'JobPosition.Closed' }),
    );
    await renderPage({ positionId });

    // Act
    type('Full name', 'John Fakeman');
    fireEvent.click(screen.getByRole('button', { name: 'Add candidate' }));

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('This job position is closed.');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('edits a candidate based on the version it loaded', async () => {
    // Arrange
    fake.on('PUT', path, noContent);
    await renderPage({ candidateId: candidate.id });
    await loaded();

    // Act
    type('Full name', 'Jane Q. Fakeperson');
    type('Profile link 1', '');
    fireEvent.click(screen.getByRole('button', { name: 'Save changes' }));

    // Assert
    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith(['/candidates', candidate.id]));
    expect(await sentBody('PUT')).toEqual({
      fullName: 'Jane Q. Fakeperson',
      email: 'jane@example.com',
      phone: null,
      links: [],
      version: 5,
    });
  });

  it('says someone else changed the candidate and reloads them on request', async () => {
    // Arrange
    fake.on('PUT', path, () =>
      json(409, { status: 409, detail: 'Changed.', code: 'Concurrency.ChangedMeanwhile' }),
    );
    await renderPage({ candidateId: candidate.id });
    await loaded();
    type('Full name', 'Fake Stale Edit');
    fireEvent.click(screen.getByRole('button', { name: 'Save changes' }));
    const reload = await screen.findByRole('button', { name: 'Reload candidate' });
    expect(screen.getByRole('alert').textContent).toContain('Someone else changed this candidate');
    fake.on('GET', path, () =>
      json(200, { ...candidate, fullName: 'Fake Their Edit', version: 6 }),
    );

    // Act
    fireEvent.click(reload);

    // Assert
    await vi.waitFor(() =>
      expect((screen.getByLabelText('Full name') as HTMLInputElement).value).toBe(
        'Fake Their Edit',
      ),
    );
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('does not offer a form when the position is closed', async () => {
    // Arrange
    fake.on('GET', path, () => json(200, { ...candidate, positionState: 'Closed' }));

    // Act
    await renderPage({ candidateId: candidate.id });

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('position is closed');
    expect(screen.queryByLabelText('Full name')).toBeNull();
  });

  it('accepts only full http and https profile links', () => {
    expect(isProfileLink('https://profiles.example.com/jane')).toBe(true);
    expect(isProfileLink('http://profiles.example.com')).toBe(true);
    expect(isProfileLink('profiles.example.com/jane')).toBe(false);
    expect(isProfileLink('ftp://files.example.com/cv')).toBe(false);
    expect(isProfileLink('javascript:alert(1)')).toBe(false);
  });
});
