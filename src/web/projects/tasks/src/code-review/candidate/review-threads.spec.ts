import type { ChangedFile, ReviewComment } from './code-review';
import {
  changedFileStatus,
  commentCounts,
  commentDecorations,
  isAnchored,
  lineRangeLabel,
  reviewThreads,
} from './review-threads';

const files: ChangedFile[] = [
  { path: 'src/Fake.cs', language: 'csharp', original: 'a\nb', modified: 'a\nb\nc' },
  { path: 'src/Added.cs', language: 'csharp', modified: 'x' },
  { path: 'src/Deleted.cs', language: 'csharp', original: 'y', modified: null },
];

function comment(overrides: Partial<ReviewComment>): ReviewComment {
  return {
    id: 'c1',
    file: 'src/Fake.cs',
    side: 'modified',
    startLine: 1,
    endLine: 1,
    body: 'A made-up remark.',
    ...overrides,
  };
}

describe('review threads', () => {
  it('derive a changed file status from the sides it has', () => {
    expect(files.map(changedFileStatus)).toEqual(['Modified', 'Added', 'Deleted']);
  });

  it('label single lines and ranges with their side', () => {
    expect(lineRangeLabel({ side: 'modified', startLine: 4, endLine: 4 })).toBe(
      'line 4 (modified)',
    );
    expect(lineRangeLabel({ side: 'original', startLine: 2, endLine: 5 })).toBe(
      'lines 2–5 (original)',
    );
  });

  it('count comments per file', () => {
    const counts = commentCounts([
      comment({ id: 'a' }),
      comment({ id: 'b' }),
      comment({ id: 'c', file: 'src/Added.cs' }),
    ]);

    expect(counts.get('src/Fake.cs')).toBe(2);
    expect(counts.get('src/Added.cs')).toBe(1);
  });

  it('decorate the lines of each comment', () => {
    expect(commentDecorations([comment({ startLine: 2, endLine: 3 })])).toEqual([
      { side: 'modified', startLine: 2, endLine: 3, kind: 'comment' },
    ]);
  });

  it('tell an anchored comment from one whose file or lines do not exist', () => {
    expect(isAnchored(comment({ endLine: 3 }), files)).toBe(true);
    expect(isAnchored(comment({ side: 'original', endLine: 3 }), files)).toBe(false);
    expect(isAnchored(comment({ file: 'src/Missing.cs' }), files)).toBe(false);
    expect(isAnchored(comment({ file: 'src/Added.cs', side: 'original' }), files)).toBe(false);
  });

  it('group a file’s comments by the side and line they end on, ordered by line', () => {
    // Act
    const threads = reviewThreads('src/Fake.cs', [
      comment({ id: 'late', startLine: 3, endLine: 3 }),
      comment({ id: 'range', startLine: 1, endLine: 3 }),
      comment({ id: 'first', startLine: 1, endLine: 1 }),
      comment({ id: 'other-side', side: 'original', startLine: 1, endLine: 1 }),
      comment({ id: 'other-file', file: 'src/Added.cs' }),
    ]);

    // Assert
    expect(threads.map((thread) => thread.id)).toEqual([
      'src/Fake.cs:modified:1',
      'src/Fake.cs:original:1',
      'src/Fake.cs:modified:3',
    ]);
    expect(threads[2].data.comments.map((c) => c.id)).toEqual(['range', 'late']);
    expect(threads.every((thread) => !thread.data.draft)).toBe(true);
  });

  it('mark the thread of a draft and add one when no comment ends there', () => {
    // Act
    const joined = reviewThreads('src/Fake.cs', [comment({})], {
      side: 'modified',
      startLine: 1,
      endLine: 1,
    });
    const alone = reviewThreads('src/Fake.cs', [comment({})], {
      side: 'original',
      startLine: 1,
      endLine: 2,
    });

    // Assert
    expect(joined).toHaveLength(1);
    expect(joined[0].data.draft).toBe(true);
    expect(alone.map((thread) => [thread.id, thread.data.draft])).toEqual([
      ['src/Fake.cs:modified:1', false],
      ['src/Fake.cs:original:2', true],
    ]);
  });
});
