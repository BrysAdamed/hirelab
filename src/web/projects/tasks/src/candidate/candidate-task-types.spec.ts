import { Component, input, output } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TaskTypeRegistry } from '@hirelab/tasks/framework';
import type { AnyCandidateTaskComponent, CandidateTaskComponent } from './candidate-task-component';
import { CANDIDATE_TASK_TYPES, candidateTaskTypes } from './candidate-task-types';

interface StubContent {
  readonly phrase: string;
}

interface StubResponse {
  readonly echo: string;
}

@Component({ selector: 'hl-stub-candidate-task', template: '' })
class StubCandidateTask implements CandidateTaskComponent<StubContent, StubResponse> {
  readonly content = input.required<StubContent>();
  readonly response = input.required<StubResponse>();
  readonly responseChange = output<StubResponse>();
}

describe('candidate task types', () => {
  it('ship the open question and code review types', () => {
    // Assert
    expect(candidateTaskTypes.keys).toEqual(['open-question', 'code-review']);
  });

  it('are injected from the shipped registry', () => {
    // Act
    const registry = TestBed.inject(CANDIDATE_TASK_TYPES);

    // Assert
    expect(registry).toBe(candidateTaskTypes);
  });

  it('accept a component that implements the candidate contract', async () => {
    // Arrange
    const registry = new TaskTypeRegistry<AnyCandidateTaskComponent>({
      'stub-type': { loadComponent: () => Promise.resolve(StubCandidateTask) },
    });
    TestBed.configureTestingModule({
      providers: [{ provide: CANDIDATE_TASK_TYPES, useValue: registry }],
    });

    // Act
    const component = await TestBed.inject(CANDIDATE_TASK_TYPES).find('stub-type')?.loadComponent();

    // Assert
    expect(component).toBe(StubCandidateTask);
  });
});
