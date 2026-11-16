import type { InputSignal, OutputEmitterRef } from '@angular/core';

/**
 * The contract of a task type's `candidate` entry point: it renders the candidate content and edits the
 * response. The app shell owns autosave, revisions, refetching and the read-only lock; the component never
 * calls the API, except a type with its own endpoints through a client scoped to its route.
 */
export interface CandidateTaskComponent<TCandidateContent = unknown, TResponse = unknown> {
  readonly content: InputSignal<TCandidateContent>;
  readonly response: InputSignal<TResponse>;
  readonly responseChange: OutputEmitterRef<TResponse>;
}

/** A candidate component of any task type. Signal inputs are invariant, so a registry needs `any` here. */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type AnyCandidateTaskComponent = CandidateTaskComponent<any, any>;
