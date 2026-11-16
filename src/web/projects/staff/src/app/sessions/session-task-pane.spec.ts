import { Component, input } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TaskTypeRegistry, type TaskContent } from '@hirelab/tasks/framework';
import {
  type AnyReviewTaskComponent,
  REVIEW_TASK_TYPES,
  type ReviewTaskComponent,
} from '@hirelab/tasks/review';
import { fireEvent, render, screen } from '@testing-library/angular';
import { STAFF_API_FETCH, type SessionTaskDetail } from '../api/staff-api';
import { FakeFetch, json } from '../testing/fake-fetch';
import { SessionTaskPane } from './session-task-pane';

interface StubContent {
  readonly phrase: string;
}

interface StubResponse {
  readonly echo: string;
}

@Component({
  selector: 'hl-stub-review-task',
  template: `
    <p>Phrase: {{ content().candidate.phrase }}</p>
    <p>Echo: {{ response()?.echo ?? 'none' }}</p>
    <p>Reference: {{ referenceAnswer() }}</p>
    <p>{{ live() ? 'Live' : 'Not live' }}</p>
  `,
})
class StubReviewTask implements ReviewTaskComponent<StubContent, unknown, StubResponse> {
  readonly content = input.required<TaskContent<StubContent, unknown>>();
  readonly response = input.required<StubResponse | null>();
  readonly referenceAnswer = input.required<string>();
  readonly interviewerNotes = input<string>();
  readonly live = input(false);
}

describe('SessionTaskPane', () => {
  const sessionId = '0190a5c4-7e3d-7b8a-9f21-000000000081';
  const task: SessionTaskDetail = {
    id: '0190a5c4-7e3d-7b8a-9f21-000000000091',
    taskId: '0190a5c4-7e3d-7b8a-9f21-000000000061',
    state: 'Current',
    versionNumber: 2,
    taskType: 'stub-type',
    title: 'Fake echo task',
    summary: 'Echo a made-up phrase.',
    referenceAnswer: 'Echo it back.',
    interviewerNotes: null,
    candidateContent: { phrase: 'Fake phrase' },
    staffContent: {},
    response: { echo: 'fake first echo' },
    responseRevision: 1,
  };
  const path = `/api/staff/sessions/${sessionId}/tasks/${task.id}`;

  let fake: FakeFetch;
  let shown: SessionTaskDetail;

  async function renderPane(options: { sessionLive?: boolean } = {}) {
    fake = new FakeFetch().on('GET', path, () => json(200, shown));
    await render(SessionTaskPane, {
      inputs: {
        sessionId,
        sessionTaskId: task.id,
        queueRevision: 1,
        sessionLive: options.sessionLive ?? true,
      },
      providers: [
        provideRouter([]),
        { provide: STAFF_API_FETCH, useValue: fake.fetch },
        {
          provide: REVIEW_TASK_TYPES,
          useValue: new TaskTypeRegistry<AnyReviewTaskComponent>({
            'stub-type': { loadComponent: () => Promise.resolve(StubReviewTask) },
          }),
        },
      ],
    });
  }

  beforeEach(() => (shown = task));

  afterEach(() => TestBed.resetTestingModule());

  it('shows the response through the task type’s review component, live while the task is current', async () => {
    // Act
    await renderPane();

    // Assert
    expect(await screen.findByText('Echo: fake first echo')).toBeTruthy();
    expect(screen.getByText('Phrase: Fake phrase')).toBeTruthy();
    expect(screen.getByText('Reference: Echo it back.')).toBeTruthy();
    expect(screen.getByText('Live')).toBeTruthy();
  });

  it('shows a done task’s response, not live', async () => {
    // Arrange
    shown = { ...task, state: 'Done' };

    // Act
    await renderPane();

    // Assert
    expect(await screen.findByText('Echo: fake first echo')).toBeTruthy();
    expect(screen.getByText('Not live')).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Refresh response' })).toBeNull();
  });

  it('refetches the response on refresh', async () => {
    // Arrange
    await renderPane();
    await screen.findByText('Echo: fake first echo');
    shown = { ...task, response: { echo: 'fake newer echo' }, responseRevision: 2 };

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Refresh response' }));

    // Assert
    expect(await screen.findByText('Echo: fake newer echo')).toBeTruthy();
  });

  it('shows the reference answer alone for a task type without a review component', async () => {
    // Arrange
    shown = { ...task, taskType: 'unknown-type' };

    // Act
    await renderPane();

    // Assert
    expect(await screen.findByText('This task type has no response view here.')).toBeTruthy();
    expect(screen.getByRole('heading', { name: 'Reference answer' })).toBeTruthy();
  });
});
