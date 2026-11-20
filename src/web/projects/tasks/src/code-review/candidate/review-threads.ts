import type { CodeLineDecoration, CodeViewZone, DiffLineRange } from '@hirelab/ui/code';
import type { ChangedFile, DiffSide, ReviewComment } from './code-review';

/** How a changed file changed: an added file has no original text, a deleted file no modified text. */
export type ChangedFileStatus = 'Added' | 'Modified' | 'Deleted';

/** The review comments that end on one line of one side of a file, and whether a new comment is being written there. */
export interface ReviewThread {
  readonly side: DiffSide;
  readonly afterLine: number;
  readonly comments: readonly ReviewComment[];
  readonly draft: boolean;
}

export function changedFileStatus(file: ChangedFile): ChangedFileStatus {
  if (file.original == null) {
    return 'Added';
  }
  return file.modified == null ? 'Deleted' : 'Modified';
}

export function lineCount(text: string | null | undefined): number {
  return text == null ? 0 : text.split('\n').length;
}

/** True when the comment's file exists and its lines are inside the side it is on. */
export function isAnchored(comment: ReviewComment, files: readonly ChangedFile[]): boolean {
  const file = files.find((candidate) => candidate.path === comment.file);
  return (
    file !== undefined &&
    comment.startLine >= 1 &&
    comment.startLine <= comment.endLine &&
    comment.endLine <= lineCount(file[comment.side])
  );
}

/** "line 4 (modified)" or "lines 4–6 (original)". */
export function lineRangeLabel(range: DiffLineRange): string {
  const lines =
    range.startLine === range.endLine
      ? `line ${range.startLine}`
      : `lines ${range.startLine}–${range.endLine}`;
  return `${lines} (${range.side})`;
}

export function commentCounts(comments: readonly ReviewComment[]): ReadonlyMap<string, number> {
  const counts = new Map<string, number>();
  for (const comment of comments) {
    counts.set(comment.file, (counts.get(comment.file) ?? 0) + 1);
  }
  return counts;
}

export function commentDecorations(comments: readonly ReviewComment[]): CodeLineDecoration[] {
  return comments.map(({ side, startLine, endLine }) => ({
    side,
    startLine,
    endLine,
    kind: 'comment',
  }));
}

/**
 * The threads of one file as diff zones: the comments grouped by the side and line they end on, ordered by line, plus
 * an empty thread for `draft` when no comment ends on its last line yet.
 */
export function reviewThreads(
  file: string,
  comments: readonly ReviewComment[],
  draft: DiffLineRange | null = null,
): CodeViewZone<ReviewThread>[] {
  const threads = new Map<
    string,
    { side: DiffSide; afterLine: number; comments: ReviewComment[] }
  >();
  const keyOf = (side: DiffSide, line: number) => `${file}:${side}:${line}`;
  const threadAt = (side: DiffSide, afterLine: number) => {
    const key = keyOf(side, afterLine);
    const thread = threads.get(key) ?? { side, afterLine, comments: [] };
    threads.set(key, thread);
    return thread;
  };
  for (const comment of comments) {
    if (comment.file === file) {
      threadAt(comment.side, comment.endLine).comments.push(comment);
    }
  }
  if (draft) {
    threadAt(draft.side, draft.endLine);
  }
  const draftKey = draft ? keyOf(draft.side, draft.endLine) : null;
  return [...threads]
    .map(([id, thread]) => ({
      id,
      side: thread.side,
      afterLine: thread.afterLine,
      data: {
        side: thread.side,
        afterLine: thread.afterLine,
        comments: [...thread.comments].sort((a, b) => a.startLine - b.startLine),
        draft: id === draftKey,
      },
    }))
    .sort((a, b) => a.afterLine - b.afterLine || a.side.localeCompare(b.side));
}
