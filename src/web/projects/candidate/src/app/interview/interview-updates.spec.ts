import { TestBed } from '@angular/core/testing';
import { FakeLiveHubConnection, provideFakeLiveHub } from '@hirelab/core/testing';
import { InterviewUpdates, type InterviewWatcher } from './interview-updates';

describe('InterviewUpdates', () => {
  let connection: FakeLiveHubConnection;
  let watcher: { changed: ReturnType<typeof vi.fn>; responseSaved: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    connection = new FakeLiveHubConnection();
    watcher = { changed: vi.fn(), responseSaved: vi.fn() };
    TestBed.configureTestingModule({ providers: [provideFakeLiveHub(connection)] });
  });

  afterEach(() => TestBed.resetTestingModule());

  function watch(): ReturnType<InterviewUpdates['watch']> {
    return TestBed.inject(InterviewUpdates).watch(watcher as InterviewWatcher);
  }

  async function settle(): Promise<void> {
    for (let i = 0; i < 3; i++) {
      await new Promise((resolve) => setTimeout(resolve));
    }
  }

  it('connects to the candidate hub and refetches once connected', async () => {
    // Act
    const running = watch();
    await settle();

    // Assert
    expect(connection.paths).toEqual(['/hubs/candidate']);
    expect(running.status()).toBe('connected');
    expect(watcher.changed).toHaveBeenCalledTimes(1);
  });

  it('refetches when the queue or the session state changes', async () => {
    // Arrange
    watch();
    await settle();
    watcher.changed.mockClear();

    // Act
    connection.send('TaskQueueChanged');
    connection.send('SessionStateChanged');

    // Assert
    expect(watcher.changed).toHaveBeenCalledTimes(2);
  });

  it('passes on a saved response', async () => {
    // Arrange
    watch();
    await settle();

    // Act
    connection.send('ResponseUpdated', 'task-1', 4);

    // Assert
    expect(watcher.responseSaved).toHaveBeenCalledWith('task-1', 4);
  });

  it('says it is reconnecting while the connection is down and refetches when it is back', async () => {
    // Arrange
    const running = watch();
    await settle();
    watcher.changed.mockClear();

    // Act
    connection.drop();
    const whileDown = running.status();
    connection.restore();
    await settle();

    // Assert
    expect(whileDown).toBe('reconnecting');
    expect(running.status()).toBe('connected');
    expect(watcher.changed).toHaveBeenCalledTimes(2);
  });

  it('keeps trying when the first connection fails', async () => {
    // Arrange
    connection.failStarts = 2;

    // Act
    const running = watch();
    await settle();

    // Assert
    expect(connection.starts).toBe(3);
    expect(running.status()).toBe('connected');
  });

  it('closes the connection when stopped', async () => {
    // Arrange
    const running = watch();
    await settle();

    // Act
    running.stop();

    // Assert
    expect(connection.stopped).toBe(true);
  });
});
