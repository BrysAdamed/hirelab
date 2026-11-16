import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { fireEvent, render, screen } from '@testing-library/angular';
import { type JobPosition, STAFF_API_FETCH } from '../api/staff-api';
import { FakeFetch, json, noContent } from '../testing/fake-fetch';
import { PositionFormPage } from './position-form-page';

describe('PositionFormPage', () => {
  const position: JobPosition = {
    id: '0190a5c4-7e3d-7b8a-9f21-000000000041',
    title: 'Fake Backend Developer',
    description: 'Builds made-up services.',
    requirements: '- Three fake years',
    niceToHaves: null,
    interviewerNotes: null,
    state: 'Open',
    createdAt: '2026-01-05T09:00:00Z',
    closedAt: null,
    closedById: null,
    closedByName: null,
    version: 7,
  };
  const path = `/api/staff/positions/${position.id}`;

  let fake: FakeFetch;
  let navigate: ReturnType<typeof vi.spyOn>;

  async function renderPage(positionId?: string) {
    await render(PositionFormPage, {
      inputs: { positionId },
      providers: [provideRouter([]), { provide: STAFF_API_FETCH, useValue: fake.fetch }],
    });
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  }

  function type(label: string | RegExp, value: string): void {
    fireEvent.input(screen.getByLabelText(label), { target: { value } });
  }

  async function sentBody(method: string): Promise<Record<string, unknown>> {
    const request = fake.requests.find((candidate) => candidate.method === method)!;
    return (await request.clone().json()) as Record<string, unknown>;
  }

  beforeEach(() => {
    fake = new FakeFetch().on('GET', path, () => json(200, position));
  });

  afterEach(() => TestBed.resetTestingModule());

  it('creates a position and opens it', async () => {
    // Arrange
    fake.on('POST', '/api/staff/positions', () => json(201, { id: position.id }));
    await renderPage();

    // Act
    type('Title', '  Fake QA Engineer ');
    type('Requirements', '- Tests fake apps');
    fireEvent.click(screen.getByRole('button', { name: 'Create position' }));

    // Assert
    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith(['/positions', position.id]));
    expect(await sentBody('POST')).toEqual({
      title: 'Fake QA Engineer',
      description: null,
      requirements: '- Tests fake apps',
      niceToHaves: null,
      interviewerNotes: null,
    });
  });

  it('asks for the requirements before sending anything', async () => {
    // Arrange
    await renderPage();

    // Act
    type('Title', 'Fake QA Engineer');
    fireEvent.click(screen.getByRole('button', { name: 'Create position' }));

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('Enter the requirements.');
    expect(fake.requests).toHaveLength(0);
  });

  it('edits a position based on the version it loaded', async () => {
    // Arrange
    fake.on('PUT', path, noContent);
    await renderPage(position.id);

    // Act
    await vi.waitFor(() =>
      expect((screen.getByLabelText('Title') as HTMLInputElement).value).toBe(position.title),
    );
    type('Title', 'Fake Senior Developer');
    fireEvent.click(screen.getByRole('button', { name: 'Save changes' }));

    // Assert
    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith(['/positions', position.id]));
    expect(await sentBody('PUT')).toMatchObject({ title: 'Fake Senior Developer', version: 7 });
  });

  it('says someone else changed the position and reloads it on request', async () => {
    // Arrange
    fake.on('PUT', path, () =>
      json(409, { status: 409, detail: 'Changed.', code: 'Concurrency.ChangedMeanwhile' }),
    );
    await renderPage(position.id);
    await vi.waitFor(() =>
      expect((screen.getByLabelText('Title') as HTMLInputElement).value).toBe(position.title),
    );
    type('Title', 'Fake Stale Edit');
    fireEvent.click(screen.getByRole('button', { name: 'Save changes' }));
    const reload = await screen.findByRole('button', { name: 'Reload position' });
    expect(screen.getByRole('alert').textContent).toContain('Someone else changed this position');
    fake.on('GET', path, () => json(200, { ...position, title: 'Fake Their Edit', version: 8 }));

    // Act
    fireEvent.click(reload);

    // Assert
    await vi.waitFor(() =>
      expect((screen.getByLabelText('Title') as HTMLInputElement).value).toBe('Fake Their Edit'),
    );
    expect(screen.queryByRole('alert')).toBeNull();
    expect(navigate).not.toHaveBeenCalled();
  });

  it('does not offer a form for a closed position', async () => {
    // Arrange
    fake.on('GET', path, () => json(200, { ...position, state: 'Closed' }));

    // Act
    await renderPage(position.id);

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('closed and read-only');
    expect(screen.queryByLabelText('Title')).toBeNull();
  });
});
