import { problemMessage } from './staff-api';

/** A failed staff API call: the message to show and, when the API sent one, the error code. */
export interface ApiFailure {
  ok: false;
  message: string;
  code?: string;
}

/** The outcome of a staff API call: its value, or the message to show. */
export type ApiResult<T> = { ok: true; value: T } | ApiFailure;

/** The code of a 409 for an edit based on a stale version: someone else saved in between. */
export const changedMeanwhileCode = 'Concurrency.ChangedMeanwhile';

const genericFailure = 'Something went wrong. Try again in a moment.';

/** A failed call's result with the ProblemDetails message (or a generic one) and its `code` extension. */
export function apiFailure(
  error: { detail?: string | null; title?: string | null } | undefined,
): ApiFailure {
  const code = (error as { code?: unknown } | undefined)?.code;
  return {
    ok: false,
    message: problemMessage(error, genericFailure),
    ...(typeof code === 'string' ? { code } : {}),
  };
}
