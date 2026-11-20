import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { candidateTaskTypes } from '@hirelab/tasks/candidate';
import { fakeMouseTargetType, provideFakeMonaco } from '@hirelab/ui/code/testing';
import { fireEvent, render, screen, within } from '@testing-library/angular';
import type { CodeReviewContent, CodeReviewResponse, ReviewComment } from './code-review';
import { CodeReviewCandidate } from './code-review-candidate';

const content: CodeReviewContent = {
  title: 'Add a fake donation handler',
  description: 'Handles **made-up** donations.',
  files: [
    {
      path: 'src/Fake/DonationHandler.cs',
      language: 'csharp',
      original: 'class A\n{\n}\n',
      modified: 'class A\n{\n    void B() { }\n}\n',
    },
    { path: 'src/Fake/DonationRules.cs', language: 'csharp', modified: 'class Rules { }\n' },
  ],
};

function fakeComment(overrides: Partial<ReviewComment> = {}): ReviewComment {
  return {
    id: '00000000-0000-7000-8000-000000000001',
    file: 'src/Fake/DonationHandler.cs',
    side: 'modified',
    startLine: 3,
    endLine: 3,
    body: 'A made-up remark.',
    ...overrides,
  };
}

@Component({
  selector: 'hl-test-host',
  imports: [CodeReviewCandidate],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <hl-code-review-candidate
      [content]="content"
      [response]="response()"
      (responseChange)="onChange($event)"
    />
  `,
})
class TestHost {
  readonly content = content;
  readonly response = signal<CodeReviewResponse | null>(null);
  readonly changes: CodeReviewResponse[] = [];

  onChange(response: CodeReviewResponse): void {
    this.changes.push(response);
    this.response.set(response);
  }
}

describe('CodeReviewCandidate', () => {
  async function setup(response: CodeReviewResponse | null = null) {
    const fake = provideFakeMonaco();
    const view = await render(TestHost, {
      providers: fake.providers,
    });
    const host = view.fixture.componentInstance;
    host.response.set(response);
    view.fixture.detectChanges();
    await vi.waitFor(() => expect(fake.monaco.diffEditors).toHaveLength(1));
    const settle = async () => {
      view.fixture.detectChanges();
      await view.fixture.whenStable();
      view.fixture.detectChanges();
    };
    await settle();
    const diff = fake.monaco.diff;
    const zones = () =>
      [...diff.modified.widgets, ...diff.original.widgets].map((widget) => widget.getDomNode());
    const zone = (index = 0) => within(zones()[index]);
    return { host, diff, settle, zones, zone, fixture: view.fixture };
  }

  function lastChange(host: TestHost): CodeReviewResponse {
    return host.changes.at(-1)!;
  }

  beforeEach(() => {
    let count = 0;
    vi.spyOn(crypto, 'randomUUID').mockImplementation(
      () =>
        `00000000-0000-7000-8000-00000000010${++count}` as `${string}-${string}-${string}-${string}-${string}`,
    );
  });

  it('is the candidate entry point of the code review type', async () => {
    // Act
    const component = await candidateTaskTypes.find('code-review')?.loadComponent();

    // Assert
    expect(component).toBe(CodeReviewCandidate);
  });

  it('shows the pull request, its files and the first file’s diff', async () => {
    // Act
    const { diff } = await setup({ comments: [fakeComment()], summary: '' });

    // Assert
    expect(screen.getByRole('heading', { name: 'Add a fake donation handler' })).toBeTruthy();
    expect(screen.getByText('made-up').tagName).toBe('STRONG');
    const files = within(screen.getByRole('navigation', { name: 'Changed files' }));
    const handler = files.getByRole('button', { name: /DonationHandler\.cs/ });
    expect(handler.textContent).toMatch(/Modified\s+1\s+comment\b/);
    expect(handler.getAttribute('aria-current')).toBe('true');
    expect(files.getByRole('button', { name: /DonationRules\.cs/ }).textContent).toMatch(
      /Added\s+0\s+comments/,
    );
    expect(diff.models?.original.getValue()).toBe('class A\n{\n}\n');
    expect(diff.models?.modified.getValue()).toContain('void B()');
    expect(diff.options['renderSideBySide']).toBe(false);
    expect(screen.getByText(/1 of\s+200 comments/)).toBeTruthy();
  });

  it('shows saved comments under their lines with highlighted lines', async () => {
    // Act
    const { diff, zone } = await setup({ comments: [fakeComment()], summary: '' });

    // Assert
    const [thread] = [...diff.modified.zones.values()];
    expect(thread.afterLineNumber).toBe(3);
    expect(zone().getByRole('article', { name: 'Comment on line 3 (modified)' })).toBeTruthy();
    expect(zone().getByText('A made-up remark.')).toBeTruthy();
    expect(diff.modified.decorations[0].range).toMatchObject({
      startLineNumber: 3,
      endLineNumber: 3,
    });
  });

  it('switches to another file', async () => {
    // Arrange
    const { diff, settle, zones } = await setup({ comments: [fakeComment()], summary: '' });

    // Act
    fireEvent.click(screen.getByRole('button', { name: /DonationRules\.cs/ }));
    await settle();

    // Assert
    expect(diff.models?.modified.getValue()).toBe('class Rules { }\n');
    expect(screen.getByRole('heading', { name: 'src/Fake/DonationRules.cs' })).toBeTruthy();
    expect(
      screen.getByRole('button', { name: /DonationRules\.cs/ }).getAttribute('aria-current'),
    ).toBe('true');
    expect(zones()).toHaveLength(0);
  });

  it('toggles the side-by-side view', async () => {
    // Arrange
    const { diff, settle } = await setup();
    const toggle = screen.getByRole('button', { name: 'Side by side' });

    // Act
    fireEvent.click(toggle);
    await settle();

    // Assert
    expect(toggle.getAttribute('aria-pressed')).toBe('true');
    expect(diff.options['renderSideBySide']).toBe(true);
  });

  it('adds a comment from the gutter button', async () => {
    // Arrange
    const { host, diff, settle, zone } = await setup();
    expect(diff.options['glyphMargin']).toBe(true);

    // Act
    diff.modified.mouseDown.fire({
      target: { type: fakeMouseTargetType.GUTTER_GLYPH_MARGIN, position: { lineNumber: 2 } },
      event: { preventDefault: () => undefined },
    });
    await settle();
    fireEvent.input(zone().getByLabelText('Comment on line 2 (modified)'), {
      target: { value: 'Brace on its own line.' },
    });
    await settle();
    fireEvent.click(zone().getByRole('button', { name: 'Add comment' }));
    await settle();

    // Assert
    expect(lastChange(host)).toEqual({
      comments: [
        {
          id: '00000000-0000-7000-8000-000000000101',
          file: 'src/Fake/DonationHandler.cs',
          side: 'modified',
          startLine: 2,
          endLine: 2,
          body: 'Brace on its own line.',
        },
      ],
      summary: '',
    });
    expect(zone().queryByRole('textbox')).toBeNull();
    expect(zone().getByText('Brace on its own line.')).toBeTruthy();
    expect(screen.getByRole('button', { name: /DonationHandler\.cs/ }).textContent).toMatch(
      /1\s+comment\b/,
    );
  });

  it('adds a comment on the selected lines with the Comment button', async () => {
    // Arrange
    const { host, diff, settle, zone } = await setup();
    diff.modified.select({ startLineNumber: 2, startColumn: 1, endLineNumber: 3, endColumn: 9 });

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Comment' }));
    await settle();
    fireEvent.input(zone().getByLabelText('Comment on lines 2–3 (modified)'), {
      target: { value: 'Extract a method.' },
    });
    await settle();
    fireEvent.click(zone().getByRole('button', { name: 'Add comment' }));

    // Assert
    expect(lastChange(host).comments[0]).toMatchObject({
      side: 'modified',
      startLine: 2,
      endLine: 3,
      body: 'Extract a method.',
    });
    expect([...diff.modified.zones.values()][0].afterLineNumber).toBe(3);
  });

  it('adds a comment with Ctrl+Alt+M and saves it with Ctrl+Enter', async () => {
    // Arrange
    const { host, diff, settle, zone } = await setup();
    expect(diff.modified.actions[0]).toMatchObject({ label: 'Add a review comment' });
    diff.modified.select({ startLineNumber: 3, startColumn: 1, endLineNumber: 3, endColumn: 5 });

    // Act
    diff.modified.actions[0].run();
    await settle();
    const field = zone().getByLabelText('Comment on line 3 (modified)');
    fireEvent.input(field, { target: { value: 'Name this method.' } });
    await settle();
    fireEvent.keyDown(field, { key: 'Enter', ctrlKey: true });

    // Assert
    expect(lastChange(host).comments[0]).toMatchObject({
      startLine: 3,
      endLine: 3,
      body: 'Name this method.',
    });
  });

  it('cancels a new comment with Escape', async () => {
    // Arrange
    const { host, diff, settle, zones, zone } = await setup();
    diff.modified.actions[0].run();
    await settle();

    // Act
    fireEvent.keyDown(zone().getByRole('textbox'), { key: 'Escape' });
    await settle();

    // Assert
    expect(zones()).toHaveLength(0);
    expect(host.changes).toEqual([]);
  });

  it('adds a comment on the original side when side by side', async () => {
    // Arrange
    const { host, diff, settle } = await setup();
    fireEvent.click(screen.getByRole('button', { name: 'Side by side' }));
    await settle();

    // Act
    diff.original.mouseDown.fire({
      target: { type: fakeMouseTargetType.GUTTER_GLYPH_MARGIN, position: { lineNumber: 1 } },
      event: { preventDefault: () => undefined },
    });
    await settle();
    const form = within(diff.original.widgets[0].getDomNode());
    fireEvent.input(form.getByLabelText('Comment on line 1 (original)'), {
      target: { value: 'Was this class sealed?' },
    });
    await settle();
    fireEvent.click(form.getByRole('button', { name: 'Add comment' }));

    // Assert
    expect(lastChange(host).comments[0]).toMatchObject({ side: 'original', startLine: 1 });
  });

  it('shows an added file inline, without the side-by-side toggle', async () => {
    // Arrange
    const { diff, settle } = await setup();
    fireEvent.click(screen.getByRole('button', { name: 'Side by side' }));
    await settle();

    // Act
    fireEvent.click(screen.getByRole('button', { name: /DonationRules\.cs/ }));
    await settle();

    // Assert
    expect(screen.queryByRole('button', { name: 'Side by side' })).toBeNull();
    expect(diff.options['renderSideBySide']).toBe(false);
  });

  it('previews a comment as Markdown before adding it', async () => {
    // Arrange
    const { diff, settle, zone } = await setup();
    diff.modified.actions[0].run();
    await settle();
    fireEvent.input(zone().getByRole('textbox'), { target: { value: 'Use **guard clauses**.' } });
    await settle();

    // Act
    fireEvent.click(zone().getByRole('button', { name: 'Preview' }));
    await settle();

    // Assert
    expect(zone().getByText('guard clauses').tagName).toBe('STRONG');
    expect(zone().getByText(/22 of\s+5,000 characters/)).toBeTruthy();
  });

  it('edits a comment', async () => {
    // Arrange
    const { host, settle, zone } = await setup({ comments: [fakeComment()], summary: 'Fine.' });

    // Act
    fireEvent.click(zone().getByRole('button', { name: 'Edit comment on line 3 (modified)' }));
    await settle();
    const field = zone().getByLabelText('Comment on line 3 (modified)') as HTMLTextAreaElement;
    expect(field.value).toBe('A made-up remark.');
    fireEvent.input(field, { target: { value: 'An edited remark.' } });
    await settle();
    fireEvent.click(zone().getByRole('button', { name: 'Save comment' }));
    await settle();

    // Assert
    expect(lastChange(host)).toEqual({
      comments: [fakeComment({ body: 'An edited remark.' })],
      summary: 'Fine.',
    });
    expect(zone().getByText('An edited remark.')).toBeTruthy();
  });

  it('deletes a comment after confirming', async () => {
    // Arrange
    const { host, settle, zones, zone } = await setup({
      comments: [fakeComment()],
      summary: '',
      verdict: 'Approve',
    });

    // Act
    fireEvent.click(zone().getByRole('button', { name: 'Delete comment on line 3 (modified)' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Delete comment' }));
    await settle();

    // Assert
    expect(lastChange(host)).toEqual({ comments: [], summary: '', verdict: 'Approve' });
    expect(zones()).toHaveLength(0);
  });

  it('writes the review summary with a preview', async () => {
    // Arrange
    const { host, settle } = await setup({ comments: [fakeComment()], summary: '' });
    const summary = screen.getByLabelText('Review summary');

    // Act
    fireEvent.input(summary, { target: { value: '- Missing *tests*' } });
    await settle();

    // Assert
    expect(lastChange(host)).toEqual({ comments: [fakeComment()], summary: '- Missing *tests*' });
    expect(summary.getAttribute('maxlength')).toBe('20000');
    expect(screen.getByText(/17 of\s+20,000 characters/)).toBeTruthy();

    // Act
    const finish = within(screen.getByRole('region', { name: 'Finish review' }));
    fireEvent.click(finish.getByRole('button', { name: 'Preview' }));
    await settle();

    // Assert
    expect(finish.getByText('tests').tagName).toBe('EM');
  });

  it('sets and clears the verdict', async () => {
    // Arrange
    const { host, settle } = await setup();
    const group = within(screen.getByRole('group', { name: 'Verdict (optional)' }));
    expect(group.getAllByRole('radio').map((radio) => radio.getAttribute('value'))).toEqual([
      'Approve',
      'RequestChanges',
      'Comment',
    ]);

    // Act
    fireEvent.click(group.getByRole('radio', { name: 'Request changes' }));
    await settle();

    // Assert
    expect(lastChange(host)).toEqual({ comments: [], summary: '', verdict: 'RequestChanges' });
    expect(
      (group.getByRole('radio', { name: 'Request changes' }) as HTMLInputElement).checked,
    ).toBe(true);

    // Act
    fireEvent.click(group.getByRole('button', { name: 'Clear verdict' }));
    await settle();

    // Assert
    expect(lastChange(host)).toEqual({ comments: [], summary: '', verdict: null });
  });

  it('stops adding comments at the limit of 200', async () => {
    // Arrange
    const comments = Array.from({ length: 200 }, (_, index) =>
      fakeComment({
        id: `00000000-0000-7000-8000-${String(index).padStart(12, '0')}`,
        file: 'src/Fake/DonationRules.cs',
        startLine: 1,
        endLine: 1,
      }),
    );

    // Act
    const { diff, settle, zones } = await setup({ comments, summary: '' });
    diff.modified.mouseDown.fire({
      target: { type: fakeMouseTargetType.GUTTER_GLYPH_MARGIN, position: { lineNumber: 1 } },
      event: { preventDefault: () => undefined },
    });
    await settle();

    // Assert
    expect((screen.getByRole('button', { name: 'Comment' }) as HTMLButtonElement).disabled).toBe(
      true,
    );
    expect(screen.getByText(/the most a review can have/)).toBeTruthy();
    expect(diff.options['glyphMargin']).toBe(false);
    expect(diff.modified.actions).toHaveLength(0);
    expect(zones()).toHaveLength(0);
  });

  it('limits a comment body to 5,000 characters and needs some text to add it', async () => {
    // Arrange
    const { diff, settle, zone } = await setup();
    diff.modified.actions[0].run();
    await settle();

    // Assert
    const field = zone().getByRole('textbox');
    expect(field.getAttribute('maxlength')).toBe('5000');
    expect(field.getAttribute('aria-describedby')).toBe(
      zone().getByText(/You can use Markdown/).id,
    );
    expect(
      (zone().getByRole('button', { name: 'Add comment' }) as HTMLButtonElement).disabled,
    ).toBe(true);
  });
});
