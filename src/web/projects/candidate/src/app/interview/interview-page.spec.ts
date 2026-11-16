import { Component, input, output, signal } from '@angular/core';
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
import type { LiveHubStatus } from '@hirelab/core/live';
import { InterviewUpdates, type InterviewWatch, type InterviewWatcher } from './interview-updates';
import { RESPONSE_AUTOSAVE_DELAY, RESPONSE_AUTOSAVE_RETRY_DELAY } from './response-autosave';

interface StubContent {
  readonly phrase: string;
}

interface StubResponse {
  readonly echo: string;
}

interface SaveBody {
  readonly sessionTaskId: string;
  readonly response: StubResponse;
  readonly revision: number;
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
  private watcher: InterviewWatcher | null = null;
  readonly status = signal<LiveHubStatus>('connected');

  watch(watcher: InterviewWatcher): InterviewWatch {
    this.watcher = watcher;
    return { status: this.status, stop: () => (this.watcher = null) };
  }

  get watching(): boolean {
    return this.watcher !== null;
  }

  change(): void {
    this.watcher?.changed();
  }

  responseSaved(sessionTaskId: string, revision: number): void {
    this.watcher?.responseSaved(sessionTaskId, revision);
  }
}

const waiting: Interview = { stage: 'Waiting', currentTask: null };
const closing: Interview = { stage: 'Closing', currentTask: null };

function currentTask(
  id: string,
  phrase: string,
  response: StubResponse | null = null,
  revision = 0,
): Interview {
  return {
    stage: 'CurrentTask',
    currentTask: { id, taskType: 'stub-type', content: { phrase }, response, revision },
  };
}

describe('InterviewPage', () => {
  let fake: FakeFetch;
  let updates: ManualUpdates;
  let interview: () => Response;
  let save: (request: Request) => Promise<Response>;

  async function saved(request: Request): Promise<Response> {
    const body = (await request.clone().json()) as SaveBody;
    return json(200, { revision: body.revision + 1 });
  }

  async function savesTo(): Promise<SaveBody[]> {
    return Promise.all(
      requestsTo('PUT', '/api/candidate/session/response').map(
        async (request) => (await request.clone().json()) as SaveBody,
      ),
    );
  }

  async function typeEcho(harness: RouterTestingHarness, echo: string): Promise<void> {
    fireEvent.input(await screen.findByLabelText('Echo'), { target: { value: echo } });
    await settle(harness);
  }

  beforeEach(() => {
    fake = new FakeFetch();
    updates = new ManualUpdates();
    interview = () => json(200, waiting);
    save = (request) => saved(request);
    fake
      .on('GET', '/api/candidate/access/antiforgery', noContent)
      .on('POST', '/api/candidate/access', noContent)
      .on('GET', '/api/candidate/session', () => interview())
      .on('PUT', '/api/candidate/session/response', (request) => save(request));
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        { provide: CANDIDATE_API_FETCH, useValue: fake.fetch },
        { provide: InterviewUpdates, useValue: updates },
        { provide: RESPONSE_AUTOSAVE_DELAY, useValue: 0 },
        { provide: RESPONSE_AUTOSAVE_RETRY_DELAY, useValue: 60_000 },
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

  it('takes a response saved in another tab when nothing here is unsaved', async () => {
    // Arrange
    interview = () => json(200, currentTask('task-1', 'Fake phrase', { echo: 'fake first' }, 1));
    const harness = await open('/');
    interview = () =>
      json(200, currentTask('task-1', 'Fake phrase', { echo: 'fake other tab' }, 2));

    // Act
    updates.responseSaved('task-1', 2);
    await settle(harness);

    // Assert
    expect((screen.getByLabelText('Echo') as HTMLInputElement).value).toBe('fake other tab');
  });

  it('doesn’t refetch for its own save', async () => {
    // Arrange
    interview = () => json(200, currentTask('task-1', 'Fake phrase', null, 0));
    const harness = await open('/');
    await typeEcho(harness, 'fake edit');
    const readsBefore = requestsTo('GET', '/api/candidate/session').length;

    // Act
    updates.responseSaved('task-1', 1);
    await settle(harness);

    // Assert
    expect(requestsTo('GET', '/api/candidate/session')).toHaveLength(readsBefore);
  });

  it('says it is reconnecting while the live connection is down', async () => {
    // Arrange
    const harness = await open('/');

    // Act
    updates.status.set('reconnecting');
    harness.detectChanges();

    // Assert
    expect(screen.getByText(/Reconnecting…/)).toBeTruthy();
  });

  it('opens the current task with its saved response', async () => {
    // Arrange
    interview = () => json(200, currentTask('task-1', 'Fake phrase', { echo: 'fake saved' }, 3));

    // Act
    await open('/');

    // Assert
    expect(((await screen.findByLabelText('Echo')) as HTMLInputElement).value).toBe('fake saved');
  });

  it('autosaves the whole response with the revision of the previous save', async () => {
    // Arrange
    interview = () => json(200, currentTask('task-1', 'Fake phrase', { echo: 'fake saved' }, 3));
    const harness = await open('/');

    // Act
    await typeEcho(harness, 'fake first edit');
    await typeEcho(harness, 'fake second edit');

    // Assert
    expect(await savesTo()).toEqual([
      { sessionTaskId: 'task-1', response: { echo: 'fake first edit' }, revision: 3 },
      { sessionTaskId: 'task-1', response: { echo: 'fake second edit' }, revision: 4 },
    ]);
    expect(screen.getByRole('status').textContent).toContain('Saved');
  });

  it('loads the newer response and says so when another tab saved first', async () => {
    // Arrange
    interview = () => json(200, currentTask('task-1', 'Fake phrase', null, 1));
    const harness = await open('/');
    save = () => Promise.resolve(json(409, { status: 409, code: 'Response.StaleRevision' }));
    interview = () =>
      json(200, currentTask('task-1', 'Fake phrase', { echo: 'fake other tab' }, 2));

    // Act
    await typeEcho(harness, 'fake stale edit');

    // Assert
    expect((screen.getByLabelText('Echo') as HTMLInputElement).value).toBe('fake other tab');
    expect(screen.getByRole('status').textContent).toContain('Updated in another tab');
    save = (request) => saved(request);
    await typeEcho(harness, 'fake next edit');
    expect((await savesTo()).at(-1)).toEqual({
      sessionTaskId: 'task-1',
      response: { echo: 'fake next edit' },
      revision: 2,
    });
  });

  it('gets a new antiforgery token and saves again when the token is refused', async () => {
    // Arrange
    interview = () => json(200, currentTask('task-1', 'Fake phrase'));
    const harness = await open('/');
    const antiforgeryBefore = requestsTo('GET', '/api/candidate/access/antiforgery').length;
    let attempts = 0;
    save = (request) =>
      ++attempts === 1
        ? Promise.resolve(json(400, { status: 400, code: 'Antiforgery.InvalidToken' }))
        : saved(request);

    // Act
    await typeEcho(harness, 'fake edit');

    // Assert
    expect(requestsTo('GET', '/api/candidate/access/antiforgery')).toHaveLength(
      antiforgeryBefore + 1,
    );
    expect(await savesTo()).toHaveLength(2);
    expect(screen.getByRole('status').textContent).toContain('Saved');
  });

  it('says the answer wasn’t saved when the save fails', async () => {
    // Arrange
    interview = () => json(200, currentTask('task-1', 'Fake phrase'));
    const harness = await open('/');
    save = () => Promise.resolve(json(500, { status: 500 }));

    // Act
    await typeEcho(harness, 'fake edit');

    // Assert
    expect(screen.getByRole('status').textContent).toContain('Not saved yet');
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
