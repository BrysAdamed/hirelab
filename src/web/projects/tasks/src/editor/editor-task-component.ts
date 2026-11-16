import type { InputSignal, OutputEmitterRef } from '@angular/core';
import type { TaskContent } from '@hirelab/tasks/framework';

/**
 * The backend's validation errors for a task version's content, keyed by field as in the `errors` of a
 * validation `ProblemDetails`, for example `candidateContent` or `candidateContent.files[0].path`.
 */
export type TaskContentErrors = Readonly<Record<string, readonly string[]>>;

/**
 * The contract of a task type's `editor` entry point (ADR 0015): it edits the type's content, both the
 * candidate part and the staff-only part, and shows the backend's validation errors. The generic editor in
 * the staff app owns everything else: metadata, visibility, rubric, reference answer and interviewer notes.
 */
export interface EditorTaskComponent<TCandidateContent = unknown, TStaffContent = unknown> {
  readonly content: InputSignal<TaskContent<TCandidateContent, TStaffContent>>;
  readonly errors: InputSignal<TaskContentErrors>;
  readonly contentChange: OutputEmitterRef<TaskContent<TCandidateContent, TStaffContent>>;
}

/** An editor component of any task type. Signal inputs are invariant, so a registry needs `any` here. */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type AnyEditorTaskComponent = EditorTaskComponent<any, any>;
