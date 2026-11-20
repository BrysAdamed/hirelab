import { InjectionToken } from '@angular/core';
import { TaskTypeRegistry } from '@hirelab/tasks/framework';
import type { AnyReviewTaskComponent } from './review-task-component';

/** The `review` entry point of every task type. Staff only. */
export const reviewTaskTypes = new TaskTypeRegistry<AnyReviewTaskComponent>({
  'open-question': {
    loadComponent: () =>
      import('../open-question/review/open-question-review').then((m) => m.OpenQuestionReview),
  },
  'code-review': {
    loadComponent: () =>
      import('../code-review/review/code-review-review').then((m) => m.CodeReviewReview),
  },
});

export const REVIEW_TASK_TYPES = new InjectionToken<TaskTypeRegistry<AnyReviewTaskComponent>>(
  'REVIEW_TASK_TYPES',
  { providedIn: 'root', factory: () => reviewTaskTypes },
);
