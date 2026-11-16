import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { fireEvent, render, screen, within } from '@testing-library/angular';
import {
  type InterviewSession,
  STAFF_API_FETCH,
  type SessionTask,
  type SessionTaskDetail,
  type TaskSummary,
} from '../api/staff-api';
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
  const liveSession: InterviewSession = {
    ...session,
    state: 'Live',
    queueRevision: 3,
    tasks: [{ ...session.tasks[0], state: 'Current' }, session.tasks[1]],
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

  function detailOf(task: SessionTask): SessionTaskDetail {
    return {
      ...task,
      referenceAnswer: `Echo **${task.title}**.`,
      interviewerNotes: 'Ask about _imaginary_ echoes.',
    };
  }

  async function renderPage(shown: InterviewSession = session) {
    fake = new FakeFetch().on('GET', path, () => json(200, shown));
    for (const task of shown.tasks) {
      fake.on('GET', `${path}/tasks/${task.id}`, () => json(200, detailOf(task)));
    }
    fake
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
    expect(items.map((item) => within(item).getAllByRole('button')[0].textContent)).toEqual([
      expect.stringContaining('Fake echo task'),
      expect.stringContaining('Fake review task'),
    ]);
    expect(within(items[0]).getByText('v3')).toBeTruthy();
    expect(within(items[0]).getByText('Pending')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Move Fake echo task up' })).toHaveProperty('disabled', true);
  });

  it('shows the selected task’s reference answer and interviewer notes, the first one at first', async () => {
    // Act
    await renderPage();

    // Assert
    expect(await screen.findByRole('heading', { name: 'Fake echo task' })).toBeTruthy();
    expect((await screen.findByText('Fake echo task', { selector: 'strong' })).tagName).toBe('STRONG');
    expect(screen.getByText('imaginary', { selector: 'em' })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Task bank' }).getAttribute('href')).toBe(
      `/tasks/${session.tasks[0].taskId}`,
    );

    // Act
    fireEvent.click(screen.getByRole('button', { name: /Fake review task\s*v1/ }));

    // Assert
    expect(await screen.findByRole('heading', { name: 'Fake review task' })).toBeTruthy();
    expect(screen.getByRole('button', { name: /Fake review task\s*v1/ }).getAttribute('aria-current')).toBe('true');
  });

  it('goes live', async () => {
    // Arrange
    await renderPage();
    fake.on('POST', `${path}/go-live`, noContent);

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Go live' }));

    // Assert
    await vi.waitFor(() =>
      expect(new URL(lastRequest('POST')!.url).pathname).toBe(`${path}/go-live`),
    );
  });

  it('can’t go live with an empty queue', async () => {
    // Act
    await renderPage({ ...session, tasks: [] });

    // Assert
    expect(await screen.findByRole('button', { name: 'Go live' })).toHaveProperty('disabled', true);
  });

  it('marks the current task done and offers no changes to it', async () => {
    // Arrange
    await renderPage(liveSession);
    fake.on('POST', `${path}/tasks/${liveSession.tasks[0].id}/done`, noContent);

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Mark Fake echo task done' }));

    // Assert
    await vi.waitFor(() =>
      expect(new URL(lastRequest('POST')!.url).pathname).toBe(`${path}/tasks/${liveSession.tasks[0].id}/done`),
    );
    expect(screen.getAllByText('Current').length).toBeGreaterThan(0);
    expect(screen.queryByRole('button', { name: 'Remove Fake echo task from the queue' })).toBeNull();
    expect(screen.getByRole('button', { name: 'Remove Fake review task from the queue' })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Move Fake review task up' })).toHaveProperty('disabled', true);
    expect(screen.getByRole('button', { name: 'Add to queue' })).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Delete' })).toBeNull();
    expect(screen.queryByLabelText('Date and time')).toBeNull();
  });

  it('finishes a live session after confirmation, naming the tasks that won’t be reached', async () => {
    // Arrange
    await renderPage(liveSession);
    fake.on('POST', `${path}/finish`, noContent);

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Finish' }));

    // Assert
    expect(
      await screen.findByText(/1 task that hasn’t started will be shown as not reached/),
    ).toBeTruthy();

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Finish session' }));

    // Assert
    await vi.waitFor(() => expect(new URL(lastRequest('POST')!.url).pathname).toBe(`${path}/finish`));
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

  it('offers no changes once the session has finished and shows pending tasks as not reached', async () => {
    // Act
    await renderPage({
      ...session,
      state: 'Finished',
      scheduledAt: '2026-01-06T09:00:00Z',
      tasks: [{ ...session.tasks[0], state: 'Done' }, session.tasks[1]],
    });

    // Assert
    expect(await screen.findByText('Finished')).toBeTruthy();
    expect(screen.getAllByText('Done').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Not reached').length).toBeGreaterThan(0);
    expect(screen.queryByRole('button', { name: 'Add to queue' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Delete' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Finish' })).toBeNull();
    expect(screen.queryByRole('button', { name: /^Move / })).toBeNull();
    expect(screen.queryByRole('button', { name: /^Mark / })).toBeNull();
    expect(screen.queryByLabelText('Date and time')).toBeNull();
  });
});
