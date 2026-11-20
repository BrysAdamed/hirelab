import { provideFakeMonaco } from '@hirelab/ui/code/testing';
import { render, within } from '@testing-library/angular';
import type { ChangedFile, ReviewComment } from './code-review';
import { CodeReviewDiff } from './code-review-diff';

const file: ChangedFile = {
  path: 'src/Fake.cs',
  language: 'csharp',
  original: 'a\nb',
  modified: 'a\nb\nc',
};

const comments: ReviewComment[] = [
  {
    id: '00000000-0000-7000-8000-000000000001',
    file: 'src/Fake.cs',
    side: 'modified',
    startLine: 2,
    endLine: 3,
    body: 'A *made-up* remark.',
  },
  {
    id: '00000000-0000-7000-8000-000000000002',
    file: 'src/Other.cs',
    side: 'modified',
    startLine: 1,
    endLine: 1,
    body: 'On another file.',
  },
];

describe('CodeReviewDiff', () => {
  it('shows the file’s comments read-only under their lines, with extra decorations on top', async () => {
    // Arrange
    const fake = provideFakeMonaco();

    // Act
    const view = await render(CodeReviewDiff, {
      providers: fake.providers,
      inputs: {
        file,
        comments,
        decorations: [{ side: 'modified', startLine: 1, endLine: 1, kind: 'finding' }],
      },
    });
    await vi.waitFor(() => expect(fake.monaco.diffEditors).toHaveLength(1));
    view.fixture.detectChanges();
    await view.fixture.whenStable();

    // Assert
    const diff = fake.monaco.diff;
    expect(diff.options['glyphMargin']).toBe(false);
    expect(diff.modified.widgets).toHaveLength(1);
    const thread = within(diff.modified.widgets[0].getDomNode());
    expect(thread.getByRole('article', { name: 'Comment on lines 2–3 (modified)' })).toBeTruthy();
    expect(thread.getByText('made-up').tagName).toBe('EM');
    expect(thread.queryByRole('button')).toBeNull();
    expect(diff.modified.decorations.map((decoration) => decoration.options['className'])).toEqual([
      'hl-code-comment-line',
      'hl-code-finding-line',
    ]);
  });
});
