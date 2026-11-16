import { problemMessage } from './staff-api';

/** The outcome of a staff API call: its value, or the message to show. */
export type ApiResult<T> = { ok: true; value: T } | { ok: false; message: string };

const genericFailure = 'Something went wrong. Try again in a moment.';

/** A failed call's result with the ProblemDetails message, or a generic one. */
export function apiFailure(error: { detail?: string | null; title?: string | null } | undefined): {
  ok: false;
  message: string;
} {
  return { ok: false, message: problemMessage(error, genericFailure) };
}
