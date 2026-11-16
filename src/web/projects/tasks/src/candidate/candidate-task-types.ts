import { InjectionToken } from '@angular/core';
import { TaskTypeRegistry } from '@hirelab/tasks/framework';
import type { AnyCandidateTaskComponent } from './candidate-task-component';

/** The `candidate` entry point of every task type, the only task UI the candidate app may load. */
export const candidateTaskTypes = new TaskTypeRegistry<AnyCandidateTaskComponent>({});

export const CANDIDATE_TASK_TYPES = new InjectionToken<TaskTypeRegistry<AnyCandidateTaskComponent>>(
  'CANDIDATE_TASK_TYPES',
  { providedIn: 'root', factory: () => candidateTaskTypes },
);
