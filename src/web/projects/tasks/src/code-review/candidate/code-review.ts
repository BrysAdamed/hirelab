/** The key of the code review task type, the backend's `CodeReviewTaskType.TaskTypeKey` (ADR 0032). */
export const codeReviewKey = 'code-review';

/** The languages a changed file may use, as the backend validates them. Each maps to a Monaco grammar. */
export const codeLanguages = [
  'csharp',
  'typescript',
  'javascript',
  'html',
  'css',
  'json',
  'sql',
  'xml',
  'yaml',
  'markdown',
  'plaintext',
] as const;

export type CodeLanguage = (typeof codeLanguages)[number];

/** A side of a changed file's diff. */
export type DiffSide = 'original' | 'modified';

/** How serious an expected finding is. */
export const findingSeverities = ['Critical', 'Major', 'Minor', 'Nit'] as const;

export type FindingSeverity = (typeof findingSeverities)[number];

/** The candidate's decision on the pull request. */
export const reviewVerdicts = ['Approve', 'RequestChanges', 'Comment'] as const;

export type ReviewVerdict = (typeof reviewVerdicts)[number];

/** The limits the backend validates, from `CodeReviewTaskType`. */
export const codeReviewLimits = {
  titleMaxLength: 200,
  descriptionMaxLength: 20_000,
  minFiles: 1,
  maxFiles: 20,
  pathMaxLength: 300,
  sideMaxBytes: 200 * 1024,
  maxExpectedFindings: 50,
  maxComments: 200,
  bodyMaxLength: 5_000,
  summaryMaxLength: 20_000,
} as const;

/** What the candidate sees: the pull request's title, its Markdown description and the changed files. */
export interface CodeReviewContent {
  readonly title: string;
  readonly description: string;
  readonly files: readonly ChangedFile[];
}

/** One file of the pull request. An added file has no original text and a deleted file no modified text. */
export interface ChangedFile {
  readonly path: string;
  readonly language: CodeLanguage;
  readonly original?: string | null;
  readonly modified?: string | null;
}

/** The staff-only part: the issues the author planted. Only the review and editor entry points use it. */
export interface CodeReviewStaffContent {
  readonly expectedFindings: readonly ExpectedFinding[];
}

/** An issue the author planted, on lines `startLine`–`endLine` (1-based, inclusive) of one side of the file at `file`. */
export interface ExpectedFinding {
  readonly id: string;
  readonly file: string;
  readonly side: DiffSide;
  readonly startLine: number;
  readonly endLine: number;
  readonly title: string;
  readonly description: string;
  readonly severity: FindingSeverity;
}

/** The candidate's review: the review comments, the Markdown review summary and an optional verdict. */
export interface CodeReviewResponse {
  readonly comments: readonly ReviewComment[];
  readonly summary: string;
  readonly verdict?: ReviewVerdict | null;
}

/**
 * A Markdown comment on lines `startLine`–`endLine` (1-based, inclusive) of one side of the file at `file`. The client
 * generates `id` (a GUID), so that whole-document autosave can edit and delete the comment. The server doesn't check
 * that the file and lines exist; the review UI lists such a comment as not anchored.
 */
export interface ReviewComment {
  readonly id: string;
  readonly file: string;
  readonly side: DiffSide;
  readonly startLine: number;
  readonly endLine: number;
  readonly body: string;
}

/** The response of a candidate who hasn't reviewed anything yet. */
export function createEmptyCodeReviewResponse(): CodeReviewResponse {
  return { comments: [], summary: '' };
}
