import { Component, input } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TaskTypeRegistry, type TaskContent } from '@hirelab/tasks/framework';
import type { AnyReviewTaskComponent, ReviewTaskComponent } from './review-task-component';
import { REVIEW_TASK_TYPES, reviewTaskTypes } from './review-task-types';

interface StubContent {
  readonly phrase: string;
}

interface StubStaffContent {
  readonly matchCriterionId: string;
}

interface StubResponse {
  readonly echo: string;
}

@Component({ selector: 'hl-stub-review-task', template: '' })
class StubReviewTask implements ReviewTaskComponent<StubContent, StubStaffContent, StubResponse> {
  readonly content = input.required<TaskContent<StubContent, StubStaffContent>>();
  readonly response = input.required<StubResponse | null>();
  readonly referenceAnswer = input.required<string>();
  readonly interviewerNotes = input<string>();
  readonly live = input(false);
}

describe('review task types', () => {
  it('ship empty until a task type is added', () => {
    // Assert
    expect(reviewTaskTypes.keys).toEqual([]);
  });

  it('are injected from the shipped registry', () => {
    // Act
    const registry = TestBed.inject(REVIEW_TASK_TYPES);

    // Assert
    expect(registry).toBe(reviewTaskTypes);
  });

  it('accept a component that implements the review contract', async () => {
    // Arrange
    const registry = new TaskTypeRegistry<AnyReviewTaskComponent>({
      'stub-type': { loadComponent: () => Promise.resolve(StubReviewTask) },
    });
    TestBed.configureTestingModule({
      providers: [{ provide: REVIEW_TASK_TYPES, useValue: registry }],
    });

    // Act
    const component = await TestBed.inject(REVIEW_TASK_TYPES).find('stub-type')?.loadComponent();

    // Assert
    expect(component).toBe(StubReviewTask);
  });
});
