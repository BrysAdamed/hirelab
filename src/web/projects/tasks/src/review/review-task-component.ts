import type { InputSignal } from '@angular/core';
import type { TaskContent } from '@hirelab/tasks/framework';

/**
 * The contract of a task type's `review` entry point: it shows the interviewer the candidate's response next
 * to the content, the reference answer and the interviewer notes, live while the session runs.
 */
export interface ReviewTaskComponent<
  TCandidateContent = unknown,
  TStaffContent = unknown,
  TResponse = unknown,
> {
  readonly content: InputSignal<TaskContent<TCandidateContent, TStaffContent>>;
  readonly response: InputSignal<TResponse | null>;
  readonly referenceAnswer: InputSignal<string>;
  readonly interviewerNotes: InputSignal<string | undefined>;
  readonly live: InputSignal<boolean>;
}

/** A review component of any task type. Signal inputs are invariant, so a registry needs `any` here. */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type AnyReviewTaskComponent = ReviewTaskComponent<any, any, any>;
