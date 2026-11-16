import { Component, input, output } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TaskTypeRegistry } from '@hirelab/tasks/framework';
import {
  type AnyCandidateTaskComponent,
  CANDIDATE_TASK_TYPES,
  type CandidateTaskComponent,
} from '@hirelab/tasks/candidate';
import { fireEvent, render, screen } from '@testing-library/angular';
import { TaskPreview } from './task-preview';

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
    <p>Saved echo: {{ response()?.echo ?? 'none' }}</p>
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

describe('TaskPreview', () => {
  const content: StubContent = { phrase: 'Hello, fake world' };

  async function renderPreview(
    taskType: string,
    loadComponent: () => Promise<typeof StubCandidateTask> = () =>
      Promise.resolve(StubCandidateTask),
  ) {
    const registry = new TaskTypeRegistry<AnyCandidateTaskComponent>({
      'stub-type': { loadComponent },
    });
    return render(TaskPreview, {
      inputs: { taskType, content },
      providers: [{ provide: CANDIDATE_TASK_TYPES, useValue: registry }],
    });
  }

  afterEach(() => TestBed.resetTestingModule());

  it('renders the task type’s candidate component with the content and no response', async () => {
    // Act
    await renderPreview('stub-type');

    // Assert
    expect(await screen.findByText('Say: Hello, fake world')).toBeTruthy();
    expect(screen.getByText('Saved echo: none')).toBeTruthy();
    expect(screen.getByText('Answers you enter here are not saved.')).toBeTruthy();
  });

  it('keeps the response locally and passes it back to the component', async () => {
    // Arrange
    const { fixture } = await renderPreview('stub-type');

    // Act
    fireEvent.input(await screen.findByLabelText('Echo'), {
      target: { value: 'Hello, fake echo' },
    });
    fixture.detectChanges();

    // Assert
    expect(await screen.findByText('Saved echo: Hello, fake echo')).toBeTruthy();
  });

  it('starts over on reset', async () => {
    // Arrange
    const { fixture } = await renderPreview('stub-type');
    fireEvent.input(await screen.findByLabelText('Echo'), {
      target: { value: 'Hello, fake echo' },
    });
    fixture.detectChanges();
    await screen.findByText('Saved echo: Hello, fake echo');

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Reset preview' }));
    fixture.detectChanges();

    // Assert
    expect(await screen.findByText('Saved echo: none')).toBeTruthy();
  });

  it('shows no preview for a task type without a candidate component', async () => {
    // Act
    await renderPreview('fake-unknown-type');

    // Assert
    expect(await screen.findByText(/No preview/)).toBeTruthy();
    expect(screen.queryByText(/Say:/)).toBeNull();
    expect(screen.queryByRole('button', { name: 'Reset preview' })).toBeNull();
  });

  it('says so when the component fails to load', async () => {
    // Act
    await renderPreview('stub-type', () => Promise.reject(new Error('Fake chunk load failure')));

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('could not be loaded');
  });
});
