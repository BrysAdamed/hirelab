import { Component, input, output } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TaskTypeRegistry } from '@hirelab/tasks/framework';
import {
  type AnyCandidateTaskComponent,
  CANDIDATE_TASK_TYPES,
  type CandidateTaskComponent,
} from '@hirelab/tasks/candidate';
import { fireEvent, screen } from '@testing-library/angular';
import { CANDIDATE_API_FETCH, type Interview } from '../api/candidate-api';
import { routes } from '../app.routes';
import { FakeFetch, json, noContent } from '../testing/fake-fetch';
import { InterviewUpdates } from './interview-updates';

interface StubContent {
  readonly phrase: string;
}

interface StubResponse {
  readonly echo: string;
}

@Component({
  selector: 'hl-stub-candidate-task',
  template: `
    <p>Say: {{ content().phrase }}</p>
    <label>
      Echo
      <input [value]="response()?.echo ?? ''" (input)="echo($any($event.target).value)" />
    </label>
  `,
})
class StubCandidateTask implements CandidateTaskComponent<StubContent, StubResponse | null> {
  readonly content = input.required<StubContent>();
  readonly response = input.required<StubResponse | null>();
  readonly responseChange = output<StubResponse | null>();

  protected echo(echo: string): void {
    this.responseChange.emit({ echo });
  }
}

class ManualUpdates {
  private onChange: (() => void) | null = null;

  watch(onChange: () => void): () => void {
    this.onChange = onChange;
    return () => (this.onChange = null);
  }

  get watching(): boolean {
    return this.onChange !== null;
  }

  change(): void {
    this.onChange?.();
  }
}

const waiting: Interview = { stage: 'Waiting', currentTask: null };
const closing: Interview = { stage: 'Closing', currentTask: null };

function currentTask(id: string, phrase: string): Interview {
  return { stage: 'CurrentTask', currentTask: { id, taskType: 'stub-type', content: { phrase } } };
}

describe('InterviewPage', () => {
  let fake: FakeFetch;
  let updates: ManualUpdates;
  let interview: () => Response;

  beforeEach(() => {
    fake = new FakeFetch();
    updates = new ManualUpdates();
    interview = () => json(200, waiting);
    fake
      .on('GET', '/api/candidate/access/antiforgery', noContent)
      .on('POST', '/api/candidate/access', noContent)
      .on('GET', '/api/candidate/session', () => interview());
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        { provide: CANDIDATE_API_FETCH, useValue: fake.fetch },
        { provide: InterviewUpdates, useValue: updates },
        {
          provide: CANDIDATE_TASK_TYPES,
          useValue: new TaskTypeRegistry<AnyCandidateTaskComponent>({
            'stub-type': { loadComponent: () => Promise.resolve(StubCandidateTask) },
          }),
        },
      ],
    });
  });

  afterEach(() => TestBed.resetTestingModule());

  async function open(url: string): Promise<RouterTestingHarness> {
    const harness = await RouterTestingHarness.create(url);
    await settle(harness);
    return harness;
  }

  async function settle(harness: RouterTestingHarness): Promise<void> {
    for (let i = 0; i < 5; i++) {
      await new Promise((resolve) => setTimeout(resolve));
      harness.detectChanges();
    }
  }

  function requestsTo(method: string, path: string): Request[] {
    return fake.requests.filter(
      (request) => request.method === method && new URL(request.url).pathname === path,
    );
  }

  it('exchanges the link token, drops it from the address and shows the waiting page', async () => {
    // Act
    await open('/fake-link-token');

    // Assert
    expect(TestBed.inject(Router).url).toBe('/');
    const [exchange] = requestsTo('POST', '/api/candidate/access');
    expect(await exchange.json()).toEqual({ token: 'fake-link-token' });
    expect(requestsTo('GET', '/api/candidate/access/antiforgery')).toHaveLength(1);
    expect(screen.getByRole('heading', { name: /hasn’t started yet/ })).toBeTruthy();
    expect(updates.watching).toBe(true);
  });

  it('reads the interview with the cookie when opened without a token', async () => {
    // Act
    await open('/');

    // Assert
    expect(requestsTo('POST', '/api/candidate/access')).toHaveLength(0);
    expect(screen.getByRole('heading', { name: /hasn’t started yet/ })).toBeTruthy();
  });

  it('switches from the waiting page to the current task when the interview changes', async () => {
    // Arrange
    const harness = await open('/');
    interview = () => json(200, currentTask('task-1', 'Hello, fake world'));

    // Act
    updates.change();
    await settle(harness);

    // Assert
    expect(await screen.findByText('Say: Hello, fake world')).toBeTruthy();
    expect(screen.queryByText(/Task \d+ of/)).toBeNull();
  });

  it('keeps the response while the same task stays current and drops it for the next task', async () => {
    // Arrange
    interview = () => json(200, currentTask('task-1', 'First fake phrase'));
    const harness = await open('/');
    fireEvent.input(await screen.findByLabelText('Echo'), { target: { value: 'fake echo' } });
    harness.detectChanges();

    // Act
    updates.change();
    await settle(harness);
    const kept = (screen.getByLabelText('Echo') as HTMLInputElement).value;
    interview = () => json(200, currentTask('task-2', 'Second fake phrase'));
    updates.change();
    await settle(harness);

    // Assert
    expect(kept).toBe('fake echo');
    expect(await screen.findByText('Say: Second fake phrase')).toBeTruthy();
    expect((screen.getByLabelText('Echo') as HTMLInputElement).value).toBe('');
  });

  it('shows the closing screen after the last task', async () => {
    // Arrange
    interview = () => json(200, closing);

    // Act
    await open('/');

    // Assert
    expect(screen.getByRole('heading', { name: 'That was the last task' })).toBeTruthy();
  });

  it('says the interview has ended and stops following it when the cookie is rejected', async () => {
    // Arrange
    const harness = await open('/');
    interview = () => json(401, { status: 401, code: 'CandidateSession.Ended' });

    // Act
    updates.change();
    await settle(harness);

    // Assert
    expect(screen.getByRole('heading', { name: 'This interview has ended' })).toBeTruthy();
    expect(updates.watching).toBe(false);
  });

  it('says the link doesn’t work when it was replaced', async () => {
    // Arrange
    fake.on('POST', '/api/candidate/access', () =>
      json(404, { status: 404, code: 'CandidateLink.Invalid' }),
    );

    // Act
    await open('/fake-old-token');

    // Assert
    expect(screen.getByRole('heading', { name: 'This link doesn’t work' })).toBeTruthy();
    expect(requestsTo('GET', '/api/candidate/session')).toHaveLength(0);
  });

  it('says the interview has ended when the link of a finished session is opened', async () => {
    // Arrange
    fake.on('POST', '/api/candidate/access', () =>
      json(409, { status: 409, code: 'CandidateSession.Ended' }),
    );

    // Act
    await open('/fake-link-token');

    // Assert
    expect(screen.getByRole('heading', { name: 'This interview has ended' })).toBeTruthy();
  });

  it('asks for the link when there is no candidate cookie', async () => {
    // Arrange
    interview = () => json(401, { status: 401 });

    // Act
    await open('/');

    // Assert
    expect(
      screen.getByRole('heading', { name: 'Open the link from your interviewer' }),
    ).toBeTruthy();
  });

  it('offers to try again when the interview can’t be loaded', async () => {
    // Arrange
    interview = () => json(500, { status: 500 });
    const harness = await open('/');
    interview = () => json(200, waiting);

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }));
    await settle(harness);

    // Assert
    expect(screen.getByRole('heading', { name: /hasn’t started yet/ })).toBeTruthy();
  });
});
