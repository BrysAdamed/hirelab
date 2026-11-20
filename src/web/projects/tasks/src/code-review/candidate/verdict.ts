import type { ReviewVerdict } from './code-review';

/** How a verdict reads in the UI. */
export const verdictLabels: Readonly<Record<ReviewVerdict, string>> = {
  Approve: 'Approve',
  RequestChanges: 'Request changes',
  Comment: 'Comment',
};
