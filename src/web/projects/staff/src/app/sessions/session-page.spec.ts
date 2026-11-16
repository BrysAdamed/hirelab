import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { fireEvent, render, screen, within } from '@testing-library/angular';
import { type InterviewSession, STAFF_API_FETCH, type TaskSummary } from '../api/staff-api';
import { FakeFetch, json, noContent } from '../testing/fake-fetch';
import { SessionPage } from './session-page';

describe('SessionPage', () => {
  const session: InterviewSession = {
    id: '0190a5c4-7e3d-7b8a-9f21-000000000081',
    number: 2,
    state: 'Planned',
    scheduledAt: null,
    candidateId: '0190a5c4-7e3d-7b8a-9f21-000000000051',
    candidateDisplayName: 'Jane Fakeperson',
    createdById: '0190a5c4-7e3d-7b8a-9f21-3c4d5e6f7a8b',
    createdByName: 'Fake Interviewer',
    createdAt: '2026-01-06T09:00:00Z',
    queueRevision: 2,
    tasks: [
      {
        id: '0190a5c4-7e3d-7b8a-9f21-000000000091',
        taskId: '0190a5c4-7e3d-7b8a-9f21-000000000061',
        state: 'Pending',
        versionNumber: 3,
        taskType: 'fake-echo',
        title: 'Fake echo task',
        summary: 'Echo a made-up phrase.',
      },
      {
        id: '0190a5c4-7e3d-7b8a-9f21-000000000092',
        taskId: '0190a5c4-7e3d-7b8a-9f21-000000000062',
        state: 'Pending',
        versionNumber: 1,
        taskType: 'fake-echo',
        title: 'Fake review task',
        summary: 'Review a made-up change.',
      },
    ],
  };
  const bankTask: TaskSummary = {
    id: '0190a5c4-7e3d-7b8a-9f21-000000000063',
    versionNumber: 1,
    taskType: 'fake-echo',
    title: 'Fake new task',
    summary: 'A task not queued yet.',
    level: 'Mid',
    estimatedMinutes: 20,
    tags: ['fake'],
    visibility: 'Team',
  };
  const path = `/api/staff/sessions/${session.id}`;

  let fake: FakeFetch;

  async function renderPage(shown: InterviewSession = session) {
    fake = new FakeFetch()
      .on('GET', path, () => json(200, shown))
      .on('GET', '/api/staff/tasks', () => json(200, [bankTask]))
      .on('GET', '/api/staff/tasks/tags', () => json(200, ['fake']))
      .on('GET', '/api/staff/tasks/types', () => json(200, ['fake-echo']));
    await render(SessionPage, {
      inputs: { sessionId: shown.id },
      providers: [provideRouter([]), { provide: STAFF_API_FETCH, useValue: fake.fetch }],
    });
  }

  function lastRequest(method: string): Request | undefined {
    return fake.requests.filter((request) => request.method === method).at(-1);
  }

  afterEach(() => TestBed.resetTestingModule());

  it('shows the session, its candidate and its queue in order with pinned versions', async () => {
    // Act
    await renderPage();

    // Assert
    expect(
      await screen.findByRole('heading', { name: 'Session 2 · Jane Fakeperson' }),
    ).toBeTruthy();
    expect(screen.getByText('Planned')).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Jane Fakeperson' }).getAttribute('href')).toBe(
      `/candidates/${session.candidateId}`,
    );
    const items = within(screen.getByRole('list', { name: 'Task queue' })).getAllByRole('listitem');
    expect(items.map((item) => within(item).getAllByRole('link')[0].textContent?.trim())).toEqual([
      'Fake echo task',
      'Fake review task',
    ]);
    expect(within(items[0]).getByText('v3')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Move Fake echo task up' })).toHaveProperty('disabled', true);
  });

  it('moves a task down by sending the new order', async () => {
    // Arrange
    await renderPage();
    fake.on('PUT', `${path}/tasks/order`, noContent);

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Move Fake echo task down' }));

    // Assert
    await vi.waitFor(() => expect(lastRequest('PUT')).toBeTruthy());
    expect(await lastRequest('PUT')?.json()).toEqual({
      sessionTaskIds: [session.tasks[1].id, session.tasks[0].id],
    });
  });

  it('removes a task from the queue', async () => {
    // Arrange
    await renderPage();
    fake.on('DELETE', `${path}/tasks/${session.tasks[1].id}`, noContent);

    // Act
    fireEvent.click(
      await screen.findByRole('button', { name: 'Remove Fake review task from the queue' }),
    );

    // Assert
    await vi.waitFor(() =>
      expect(new URL(lastRequest('DELETE')!.url).pathname).toBe(`${path}/tasks/${session.tasks[1].id}`),
    );
  });

  it('adds a task picked from the task bank and marks queued tasks', async () => {
    // Arrange
    await renderPage({ ...session, tasks: [{ ...session.tasks[0], taskId: bankTask.id }] });
    fake.on('POST', `${path}/tasks`, () => json(201, { id: '0190a5c4-7e3d-7b8a-9f21-000000000093' }));

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Add to queue' }));
    expect(await screen.findByText('Queued')).toBeTruthy();
    fake.on('GET', '/api/staff/tasks', () => json(200, [{ ...bankTask, id: 'other-task' }]));
    fireEvent.click(await screen.findByRole('button', { name: 'Search' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Add Fake new task to the queue' }));

    // Assert
    await vi.waitFor(() => expect(lastRequest('POST')).toBeTruthy());
    expect(await lastRequest('POST')?.json()).toEqual({ taskId: 'other-task' });
  });

  it('shows the error of a refused change', async () => {
    // Arrange
    await renderPage();
    fake.on('PUT', `${path}/tasks/order`, () =>
      json(409, { status: 409, detail: 'The new order is stale.', code: 'InterviewSession.OrderMismatch' }),
    );

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Move Fake review task up' }));

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('The new order is stale.');
  });

  it('deletes a planned session after confirmation and goes back to the candidate', async () => {
    // Arrange
    await renderPage();
    fake.on('DELETE', path, noContent);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Delete' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Delete session' }));

    // Assert
    await vi.waitFor(() =>
      expect(navigate).toHaveBeenCalledWith(['/candidates', session.candidateId]),
    );
  });

  it('offers no changes once the session has started', async () => {
    // Act
    await renderPage({ ...session, state: 'Live', scheduledAt: '2026-01-06T09:00:00Z' });

    // Assert
    expect(await screen.findByText('Live')).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Add to queue' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Delete' })).toBeNull();
    expect(screen.queryByRole('button', { name: /^Move / })).toBeNull();
    expect(screen.queryByLabelText('Date and time')).toBeNull();
  });
});
