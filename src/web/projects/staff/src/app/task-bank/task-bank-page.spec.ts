import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { fireEvent, render, screen, within } from '@testing-library/angular';
import { STAFF_API_FETCH, type TaskSummary } from '../api/staff-api';
import { FakeFetch, json } from '../testing/fake-fetch';
import type { TaskBankQuery } from './task-bank-filter';
import { TaskBankPage } from './task-bank-page';

describe('TaskBankPage', () => {
  const echo: TaskSummary = {
    id: '0190a5c4-7e3d-7b8a-9f21-000000000061',
    versionNumber: 2,
    taskType: 'fake-echo',
    title: 'Fake echo task',
    summary: 'Echo a made-up phrase back to the interviewer.',
    level: 'Mid',
    estimatedMinutes: 30,
    tags: ['csharp', 'fake'],
    visibility: 'Team',
  };
  const system: TaskSummary = {
    ...echo,
    id: '0190a5c4-7e3d-7b8a-9f21-000000000062',
    title: 'Fake system task',
    level: 'Senior',
    tags: ['sql'],
    visibility: 'System',
  };

  let fake: FakeFetch;

  async function renderPage(inputs: TaskBankQuery = {}, listed: TaskSummary[] = [echo, system]) {
    fake = new FakeFetch()
      .on('GET', '/api/staff/tasks', () => json(200, listed))
      .on('GET', '/api/staff/tasks/tags', () => json(200, ['csharp', 'fake', 'sql']))
      .on('GET', '/api/staff/tasks/types', () => json(200, ['fake-echo']));
    await render(TaskBankPage, {
      inputs,
      providers: [provideRouter([]), { provide: STAFF_API_FETCH, useValue: fake.fetch }],
    });
  }

  function listRequest(): URLSearchParams {
    const requests = fake.requests.filter(
      (request) => new URL(request.url).pathname === '/api/staff/tasks',
    );
    return new URL(requests[requests.length - 1].url).searchParams;
  }

  afterEach(() => TestBed.resetTestingModule());

  it('lists each task with a link, a clamped summary, its facts and its badge', async () => {
    // Act
    await renderPage();

    // Assert
    const link = await screen.findByRole('link', { name: 'Fake echo task' });
    expect(link.getAttribute('href')).toBe(`/tasks/${echo.id}`);
    const item = link.closest('li')!;
    expect(within(item).getByText(echo.summary).classList).toContain('line-clamp-2');
    expect(within(item).getByText('fake-echo')).toBeTruthy();
    expect(within(item).getByText('Mid')).toBeTruthy();
    expect(within(item).getByText('30 min')).toBeTruthy();
    expect(within(item).getByText('csharp')).toBeTruthy();
    expect(within(item).getByText('Team')).toBeTruthy();
    const systemItem = screen.getByRole('link', { name: 'Fake system task' }).closest('li')!;
    expect(within(systemItem).getByText('System')).toBeTruthy();
    expect(screen.getByText('2 tasks')).toBeTruthy();
  });

  it('says when the task bank is empty', async () => {
    // Act
    await renderPage({}, []);

    // Assert
    expect(await screen.findByText('The task bank is empty.')).toBeTruthy();
  });

  it('sends the filters from the query parameters to the API', async () => {
    // Act
    await renderPage({
      tag: ['csharp', 'fake'],
      level: 'Senior',
      type: 'fake-echo',
      search: 'echo',
    });

    // Assert
    await screen.findByRole('link', { name: 'Fake echo task' });
    const query = listRequest();
    expect(query.getAll('tag')).toEqual(['csharp', 'fake']);
    expect(query.getAll('level')).toEqual(['Senior']);
    expect(query.get('taskType')).toBe('fake-echo');
    expect(query.get('search')).toBe('echo');
    expect(screen.getByRole('button', { name: 'csharp' }).getAttribute('aria-pressed')).toBe(
      'true',
    );
    expect(screen.getByRole('button', { name: 'Senior' }).getAttribute('aria-pressed')).toBe(
      'true',
    );
    expect((screen.getByLabelText('Task type') as HTMLSelectElement).value).toBe('fake-echo');
    expect((screen.getByLabelText('Search') as HTMLInputElement).value).toBe('echo');
  });

  it('says when no task matches the filters', async () => {
    // Act
    await renderPage({ level: 'Junior' }, []);

    // Assert
    expect(await screen.findByText('No tasks match these filters.')).toBeTruthy();
  });

  it('puts a chosen level and tag into the query parameters', async () => {
    // Arrange
    await renderPage({ level: 'Mid' });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Junior' }));
    fireEvent.click(await screen.findByRole('button', { name: 'sql' }));

    // Assert
    expect(navigate).toHaveBeenNthCalledWith(1, ['/tasks'], {
      queryParams: { tag: null, level: ['Junior', 'Mid'], type: null, search: null },
    });
    expect(navigate).toHaveBeenNthCalledWith(2, ['/tasks'], {
      queryParams: { tag: ['sql'], level: ['Mid'], type: null, search: null },
    });
  });

  it('removes a level that is clicked again', async () => {
    // Arrange
    await renderPage({ level: ['Junior', 'Mid'] });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Junior' }));

    // Assert
    expect(navigate).toHaveBeenCalledWith(['/tasks'], {
      queryParams: { tag: null, level: ['Mid'], type: null, search: null },
    });
  });

  it('applies the search when submitted and the task type when chosen', async () => {
    // Arrange
    await renderPage();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    await screen.findByRole('option', { name: 'fake-echo' });

    // Act
    fireEvent.input(screen.getByLabelText('Search'), { target: { value: '  parrot  ' } });
    fireEvent.click(screen.getByRole('button', { name: 'Search' }));
    fireEvent.change(screen.getByLabelText('Task type'), { target: { value: 'fake-echo' } });

    // Assert
    expect(navigate).toHaveBeenNthCalledWith(1, ['/tasks'], {
      queryParams: { tag: null, level: null, type: null, search: 'parrot' },
    });
    expect(navigate).toHaveBeenNthCalledWith(2, ['/tasks'], {
      queryParams: { tag: null, level: null, type: 'fake-echo', search: null },
    });
  });

  it('clears every filter', async () => {
    // Arrange
    await renderPage({ tag: 'sql', level: 'Senior', search: 'fake' });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Clear filters' }));

    // Assert
    expect(navigate).toHaveBeenCalledWith(['/tasks'], {
      queryParams: { tag: null, level: null, type: null, search: null },
    });
  });

  it('shows why the list could not be loaded', async () => {
    // Arrange
    fake = new FakeFetch().on('GET', '/api/staff/tasks', () =>
      json(403, { status: 403, detail: 'You can’t open the task bank.' }),
    );

    // Act
    await render(TaskBankPage, {
      providers: [provideRouter([]), { provide: STAFF_API_FETCH, useValue: fake.fetch }],
    });

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('can’t open the task bank');
  });
});
