import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import { type BankTask, STAFF_API_FETCH } from '../api/staff-api';
import { FakeFetch, json } from '../testing/fake-fetch';
import { TaskDetailPage } from './task-detail-page';

describe('TaskDetailPage', () => {
  const task: BankTask = {
    id: '0190a5c4-7e3d-7b8a-9f21-000000000061',
    versionNumber: 3,
    taskType: 'fake-echo',
    title: 'Fake echo task',
    summary: 'Echo a made-up phrase back to the interviewer.',
    level: 'Mid',
    estimatedMinutes: 30,
    tags: ['fake'],
    visibility: 'Global',
    referenceAnswer: 'Echo the **made-up** phrase.',
    interviewerNotes: 'Ask about _imaginary_ echoes.',
    rubric: {
      criteria: [
        {
          type: 'scale',
          id: 'fake-clarity',
          title: 'Fake clarity',
          description: 'How clear the fake echo is.',
          weight: 3,
          levels: [{ score: 4, description: 'Crystal fake.' }],
        },
        {
          type: 'check',
          id: 'fake-echo',
          title: 'Fake echo',
          description: 'Echoed the phrase?',
          expected: 'yes',
          critical: true,
        },
      ],
    },
    candidateContent: { phrase: 'Hello, fake world' },
    staffContent: { matchCriterionId: 'fake-echo' },
  };

  async function renderPage(shown: BankTask) {
    const fake = new FakeFetch().on('GET', `/api/staff/tasks/${shown.id}`, () => json(200, shown));
    await render(TaskDetailPage, {
      inputs: { taskId: shown.id },
      providers: [provideRouter([]), { provide: STAFF_API_FETCH, useValue: fake.fetch }],
    });
  }

  afterEach(() => TestBed.resetTestingModule());

  it('shows the full summary, the facts, the version and the badge', async () => {
    // Act
    await renderPage(task);

    // Assert
    expect(await screen.findByRole('heading', { name: 'Fake echo task' })).toBeTruthy();
    expect(screen.getByText(task.summary).classList).not.toContain('line-clamp-2');
    expect(screen.getByText('v3')).toBeTruthy();
    expect(screen.getByText('Global')).toBeTruthy();
    expect(screen.getByText('30 min')).toBeTruthy();
  });

  it('renders the reference answer and interviewer notes as Markdown', async () => {
    // Act
    await renderPage(task);

    // Assert
    expect(await screen.findByRole('heading', { name: 'Reference answer' })).toBeTruthy();
    expect(screen.getByText('made-up').tagName).toBe('STRONG');
    expect(screen.getByRole('heading', { name: 'Interviewer notes' })).toBeTruthy();
    expect(screen.getByText('imaginary').tagName).toBe('EM');
  });

  it('leaves out empty interviewer notes', async () => {
    // Act
    await renderPage({ ...task, interviewerNotes: null });

    // Assert
    await screen.findByRole('heading', { name: 'Reference answer' });
    expect(screen.queryByRole('heading', { name: 'Interviewer notes' })).toBeNull();
  });

  it('shows the rubric criteria', async () => {
    // Act
    await renderPage(task);

    // Assert
    expect(await screen.findByText('Fake clarity')).toBeTruthy();
    expect(screen.getByText('Scale 0–4, weight 3')).toBeTruthy();
    expect(screen.getByText('Crystal fake.')).toBeTruthy();
    expect(screen.getByText(/expected Yes/)).toBeTruthy();
    expect(screen.getByText('Critical')).toBeTruthy();
  });

  it('offers no preview while the task type has no candidate component', async () => {
    // Act
    await renderPage(task);

    // Assert
    expect(await screen.findByRole('heading', { name: 'Candidate preview' })).toBeTruthy();
    expect(await screen.findByText(/No preview/)).toBeTruthy();
  });

  it('shows why the task could not be loaded', async () => {
    // Arrange
    const fake = new FakeFetch().on('GET', `/api/staff/tasks/${task.id}`, () =>
      json(404, { status: 404, detail: 'The task was not found.', code: 'Task.NotFound' }),
    );

    // Act
    await render(TaskDetailPage, {
      inputs: { taskId: task.id },
      providers: [provideRouter([]), { provide: STAFF_API_FETCH, useValue: fake.fetch }],
    });

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('The task was not found.');
  });
});
