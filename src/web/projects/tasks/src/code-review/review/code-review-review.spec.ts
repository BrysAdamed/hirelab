import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import type { TaskContent } from '@hirelab/tasks/framework';
import { reviewTaskTypes } from '@hirelab/tasks/review';
import { provideFakeMonaco } from '@hirelab/ui/code/testing';
import { fireEvent, render, screen, within } from '@testing-library/angular';
import type {
  CodeReviewContent,
  CodeReviewResponse,
  CodeReviewStaffContent,
  ExpectedFinding,
  ReviewComment,
} from '../candidate/code-review';
import { CodeReviewReview } from './code-review-review';

const handlerPath = 'src/Fake/DonationHandler.cs';
const rulesPath = 'src/Fake/DonationRules.cs';

function fakeContent(): TaskContent<CodeReviewContent, CodeReviewStaffContent> {
  return {
    candidate: {
      title: 'Add a fake donation handler',
      description: 'Handles **made-up** donations.',
      files: [
        {
          path: handlerPath,
          language: 'csharp',
          original: 'class A\n{\n}\n',
          modified: 'class A\n{\n    void B() { }\n}\n',
        },
        { path: rulesPath, language: 'csharp', modified: 'class Rules\n{\n}\n' },
      ],
    },
    staff: {
      expectedFindings: [
        fakeFinding({
          id: 'finding-minor',
          title: 'Fake naming slip',
          severity: 'Minor',
          file: handlerPath,
          startLine: 3,
          endLine: 3,
        }),
        fakeFinding({
          id: 'finding-critical',
          title: 'Fake missing validation',
          severity: 'Critical',
          file: rulesPath,
          startLine: 2,
          endLine: 3,
          description: 'The rules accept a *made-up* negative amount.',
        }),
      ],
    },
  };
}

function fakeFinding(overrides: Partial<ExpectedFinding> = {}): ExpectedFinding {
  return {
    id: 'finding-1',
    file: handlerPath,
    side: 'modified',
    startLine: 1,
    endLine: 1,
    title: 'A fake finding',
    description: 'A made-up description.',
    severity: 'Major',
    ...overrides,
  };
}

function fakeComment(overrides: Partial<ReviewComment> = {}): ReviewComment {
  return {
    id: '00000000-0000-7000-8000-000000000001',
    file: handlerPath,
    side: 'modified',
    startLine: 3,
    endLine: 3,
    body: 'A made-up remark.',
    ...overrides,
  };
}

@Component({
  selector: 'hl-test-host',
  imports: [CodeReviewReview],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <hl-code-review-review
      [content]="content()"
      [response]="response()"
      referenceAnswer="A strong review spots the *fake* validation gap."
      [interviewerNotes]="interviewerNotes()"
      [live]="live()"
    />
  `,
})
class TestHost {
  readonly content = signal(fakeContent());
  readonly response = signal<CodeReviewResponse | null>(null);
  readonly interviewerNotes = signal<string | undefined>(undefined);
  readonly live = signal(false);
}

function fileButton(name: RegExp): HTMLElement {
  return within(screen.getByRole('navigation', { name: 'Changed files' })).getByRole('button', {
    name,
  });
}

describe('CodeReviewReview', () => {
  async function setup(
    response: CodeReviewResponse | null,
    options: { live?: boolean; interviewerNotes?: string } = {},
  ) {
    const fake = provideFakeMonaco();
    const view = await render(TestHost, { providers: fake.providers });
    const host = view.fixture.componentInstance;
    host.response.set(response);
    host.live.set(options.live ?? false);
    host.interviewerNotes.set(options.interviewerNotes);
    view.fixture.detectChanges();
    await vi.waitFor(() => expect(fake.monaco.diffEditors).toHaveLength(1));
    const settle = async () => {
      view.fixture.detectChanges();
      await view.fixture.whenStable();
      view.fixture.detectChanges();
    };
    await settle();
    const diff = fake.monaco.diff;
    const findingsToggle = () => screen.getByRole('button', { name: 'Show expected findings' });
    const findingClasses = () =>
      [...diff.original.decorations, ...diff.modified.decorations]
        .map((decoration) => decoration.options['className'])
        .filter((name) => name === 'hl-code-finding-line');
    return { host, diff, settle, findingsToggle, findingClasses };
  }

  it('is the review entry point of the code review type', async () => {
    // Act
    const component = await reviewTaskTypes.find('code-review')?.loadComponent();

    // Assert
    expect(component).toBe(CodeReviewReview);
  });

  it('shows the pull request, the comments read-only under their lines, the summary and the verdict', async () => {
    // Act
    const { diff } = await setup(
      { comments: [fakeComment()], summary: 'A **fake** summary.', verdict: 'RequestChanges' },
      { interviewerNotes: 'Ask a made-up follow-up.' },
    );

    // Assert
    expect(screen.getByRole('heading', { name: 'Add a fake donation handler' })).toBeTruthy();
    const files = within(screen.getByRole('navigation', { name: 'Changed files' }));
    expect(files.getByRole('button', { name: /DonationHandler\.cs/ }).textContent).toMatch(
      /Modified\s+1\s+comment\b/,
    );
    expect(diff.models?.modified.getValue()).toContain('void B()');
    const thread = within(diff.modified.widgets[0].getDomNode());
    expect(thread.getByRole('article', { name: 'Comment on line 3 (modified)' })).toBeTruthy();
    expect(thread.queryByRole('button')).toBeNull();
    expect(screen.getByText('fake', { selector: 'strong' })).toBeTruthy();
    expect(screen.getByText('Verdict: Request changes')).toBeTruthy();
    expect(screen.getByRole('heading', { name: 'Reference answer' })).toBeTruthy();
    expect(screen.getByText('fake', { selector: 'em' })).toBeTruthy();
    expect(screen.getByText('Ask a made-up follow-up.')).toBeTruthy();
    expect(screen.queryByRole('heading', { name: /Not anchored/ })).toBeNull();
  });

  it('hides the expected findings by default while the session is live', async () => {
    // Act
    const { findingsToggle, findingClasses } = await setup(
      { comments: [], summary: '' },
      { live: true },
    );

    // Assert
    expect(findingsToggle().getAttribute('aria-pressed')).toBe('false');
    expect(screen.queryByRole('heading', { name: /Expected findings/ })).toBeNull();
    expect(findingClasses()).toHaveLength(0);
  });

  it('shows the expected findings by default once the session is over, most severe first', async () => {
    // Act
    const { diff, findingsToggle } = await setup({ comments: [], summary: '' });

    // Assert
    expect(findingsToggle().getAttribute('aria-pressed')).toBe('true');
    const list = screen.getByRole('heading', { name: 'Expected findings (2)' }).parentElement!;
    const items = within(list).getAllByRole('listitem');
    expect(items[0].textContent).toMatch(/Critical\s+Fake missing validation/);
    expect(items[0].textContent).toContain(`${rulesPath}, lines 2–3 (modified)`);
    expect(within(items[0]).getByText('made-up').tagName).toBe('EM');
    expect(items[1].textContent).toMatch(/Minor\s+Fake naming slip/);
    const finding = diff.modified.decorations.find(
      (decoration) => decoration.options['className'] === 'hl-code-finding-line',
    );
    expect(finding?.range).toMatchObject({ startLineNumber: 3, endLineNumber: 3 });
    expect(finding?.options['hoverMessage']).toEqual({
      value: 'Expected finding (Minor): Fake naming slip',
    });
  });

  it('shows the expected findings when the session stops being live', async () => {
    // Arrange
    const { host, settle, findingsToggle, findingClasses } = await setup(null, { live: true });

    // Act
    host.live.set(false);
    await settle();

    // Assert
    expect(findingsToggle().getAttribute('aria-pressed')).toBe('true');
    expect(findingClasses()).toHaveLength(1);
  });

  it('keeps the interviewer’s choice when the session stops being live', async () => {
    // Arrange
    const { host, settle, findingsToggle, findingClasses } = await setup(null, { live: true });
    fireEvent.click(findingsToggle());
    await settle();
    fireEvent.click(findingsToggle());
    await settle();

    // Act
    host.live.set(false);
    await settle();

    // Assert
    expect(findingsToggle().getAttribute('aria-pressed')).toBe('false');
    expect(findingClasses()).toHaveLength(0);
  });

  it('lists comments whose file or lines don’t exist as not anchored, apart from the diff', async () => {
    // Act
    const { diff } = await setup({
      comments: [
        fakeComment(),
        fakeComment({ id: 'missing-file', file: 'src/Fake/Gone.cs', body: 'On a fake gone file.' }),
        fakeComment({ id: 'missing-line', startLine: 40, endLine: 41, body: 'Past the end.' }),
      ],
      summary: '',
    });

    // Assert
    const section = screen.getByRole('heading', { name: 'Not anchored (2)' }).parentElement!;
    expect(within(section).getByText('src/Fake/Gone.cs')).toBeTruthy();
    expect(within(section).getByText('On a fake gone file.')).toBeTruthy();
    expect(within(section).getByText('Past the end.')).toBeTruthy();
    expect(fileButton(/DonationHandler\.cs/).textContent).toMatch(/Modified\s+1\s+comment\b/);
    expect(diff.modified.widgets).toHaveLength(1);
  });

  it('says the candidate hasn’t started while the session is live and there is no response', async () => {
    // Act
    await setup(null, { live: true });

    // Assert
    expect(screen.getByText('The candidate hasn’t started the review yet.')).toBeTruthy();
    expect(screen.getByText('No comments yet.')).toBeTruthy();
    expect(screen.getByText('No summary yet.')).toBeTruthy();
    expect(screen.getByText('No verdict')).toBeTruthy();
  });

  it('says there was no review once the session is over and there is no response', async () => {
    // Act
    await setup(null);

    // Assert
    expect(screen.getByText('The candidate didn’t review this pull request.')).toBeTruthy();
    expect(screen.getByText('No comments.')).toBeTruthy();
    expect(screen.getByText('No summary.')).toBeTruthy();
    expect(screen.queryByRole('heading', { name: 'Interviewer notes' })).toBeNull();
  });

  it('selects the file of a clicked expected finding and reveals its line', async () => {
    // Arrange
    const { diff, settle } = await setup({ comments: [], summary: '' });

    // Act
    fireEvent.click(screen.getByRole('button', { name: /Fake missing validation/ }));
    await settle();

    // Assert
    expect(fileButton(/DonationRules\.cs/).getAttribute('aria-current')).toBe('true');
    expect(diff.models?.modified.getValue()).toBe('class Rules\n{\n}\n');
    expect(diff.modified.revealed).toEqual([2]);
  });

  it('keeps the selected file and the toggles when a new response arrives', async () => {
    // Arrange
    const { host, diff, settle, findingsToggle } = await setup(
      { comments: [], summary: '' },
      { live: true },
    );
    fireEvent.click(screen.getByRole('button', { name: 'Side by side' }));
    fireEvent.click(fileButton(/DonationRules\.cs/));
    fireEvent.click(findingsToggle());
    await settle();

    // Act
    host.content.set(fakeContent());
    host.response.set({
      comments: [fakeComment({ file: rulesPath, startLine: 1, endLine: 1 })],
      summary: 'A fake summary.',
    });
    await settle();

    // Assert
    expect(fileButton(/DonationRules\.cs/).getAttribute('aria-current')).toBe('true');
    expect(findingsToggle().getAttribute('aria-pressed')).toBe('true');
    expect(diff.modified.widgets).toHaveLength(1);
    expect(screen.getByText('A fake summary.')).toBeTruthy();
    fireEvent.click(fileButton(/DonationHandler\.cs/));
    await settle();
    expect(diff.options['renderSideBySide']).toBe(true);
  });
});
