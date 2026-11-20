import type { DiffSide } from './code-types';

/** The shape of Monaco's `ILineChange`: an end line of 0 means nothing on that side. */
export interface LineChange {
  readonly originalStartLineNumber: number;
  readonly originalEndLineNumber: number;
  readonly modifiedStartLineNumber: number;
  readonly modifiedEndLineNumber: number;
}

export interface MappedLine {
  readonly line: number;
  readonly deleted: boolean;
}

/**
 * Maps an original line to the modified line that shows it in the inline diff. A deleted line maps to the last
 * modified line of its change (the inline diff shows the deleted block just above it).
 */
export function mapOriginalLine(line: number, changes: readonly LineChange[]): MappedLine {
  let delta = 0;
  for (const change of changes) {
    const deletes = change.originalEndLineNumber > 0;
    const modifiedEnd = change.modifiedEndLineNumber || change.modifiedStartLineNumber;
    if (deletes && line >= change.originalStartLineNumber && line <= change.originalEndLineNumber) {
      return { line: modifiedEnd, deleted: true };
    }
    const originalEnd = deletes ? change.originalEndLineNumber : change.originalStartLineNumber;
    if (line <= originalEnd) {
      break;
    }
    delta = modifiedEnd - originalEnd;
  }
  return { line: line + delta, deleted: false };
}

/**
 * Finds the original line under a point in the inline diff's deleted block, which Monaco renders as a view zone after
 * `afterLine` of the modified editor. `offset` is the 0-based line inside the block.
 */
export function deletedLineAt(
  afterLine: number,
  offset: number,
  changes: readonly LineChange[],
): number | null {
  const change = changes.find(
    (c) =>
      c.originalEndLineNumber > 0 &&
      (c.modifiedEndLineNumber > 0 ? c.modifiedStartLineNumber - 1 : c.modifiedStartLineNumber) ===
        afterLine,
  );
  if (!change) {
    return null;
  }
  return Math.min(
    change.originalStartLineNumber + Math.max(offset, 0),
    change.originalEndLineNumber,
  );
}

/** Where a line of one side is shown: in side-by-side mode on its own side, inline always in the modified editor. */
export function placeLine(
  side: DiffSide,
  line: number,
  inline: boolean,
  changes: readonly LineChange[],
): { editor: DiffSide } & MappedLine {
  if (!inline || side === 'modified') {
    return { editor: side, line, deleted: false };
  }
  return { editor: 'modified', ...mapOriginalLine(line, changes) };
}

/** Turns a Monaco selection into a line range, dropping a last line the selection only touches at column 1. */
export function selectionLines(selection: {
  startLineNumber: number;
  endLineNumber: number;
  endColumn: number;
}): { startLine: number; endLine: number } {
  const endLine =
    selection.endLineNumber > selection.startLineNumber && selection.endColumn === 1
      ? selection.endLineNumber - 1
      : selection.endLineNumber;
  return { startLine: selection.startLineNumber, endLine };
}
