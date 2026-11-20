import { editorTaskTypes, type TaskContentErrors } from '@hirelab/tasks/editor';
import type { TaskContent } from '@hirelab/tasks/framework';
import { provideFakeMonaco } from '@hirelab/ui/code/testing';
import { fireEvent, render, screen, within } from '@testing-library/angular';
import type {
  ChangedFile,
  CodeReviewContent,
  CodeReviewStaffContent,
  ExpectedFinding,
} from '../candidate/code-review';
import { CodeReviewEditor } from './code-review-editor';

type Content = TaskContent<CodeReviewContent, CodeReviewStaffContent>;

const fakeFile: ChangedFile = {
  path: 'src/fake-cart.ts',
  language: 'typescript',
  original: 'const total = 1;',
  modified: 'const total = 2;',
};

const fakeFinding: ExpectedFinding = {
  id: 'wrong-total',
  file: 'src/fake-cart.ts',
  side: 'modified',
  startLine: 1,
  endLine: 1,
  title: 'Wrong total',
  description: 'The total is made up.',
  severity: 'Major',
};

function buildContent(
  candidate: Partial<CodeReviewContent> = {},
  expectedFindings: readonly ExpectedFinding[] = [fakeFinding],
): Content {
  return {
    candidate: {
      title: 'Add a fake cart',
      description: 'Adds a **made-up** cart.',
      files: [fakeFile],
      ...candidate,
    },
    staff: { expectedFindings },
  };
}

describe('CodeReviewEditor', () => {
  async function renderEditor(content: Content = buildContent(), errors: TaskContentErrors = {}) {
    const fake = provideFakeMonaco();
    const contentChange = vi.fn();
    const view = await render(CodeReviewEditor, {
      providers: fake.providers,
      inputs: { content, errors },
      on: { contentChange },
    });
    return { ...view, ...fake, contentChange };
  }

  function lastContent(contentChange: ReturnType<typeof vi.fn>): Content {
    return contentChange.mock.lastCall?.[0] as Content;
  }

  const file = (number: number) => screen.getByRole('group', { name: `Changed file ${number}` });
  const finding = (number: number) =>
    screen.getByRole('group', { name: `Expected finding ${number}` });

  it('is the editor entry point of the code review type', async () => {
    // Act
    const component = await editorTaskTypes.find('code-review')?.loadComponent();

    // Assert
    expect(component).toBe(CodeReviewEditor);
  });

  describe('pull request', () => {
    it('shows the title and description', async () => {
      // Act
      await renderEditor();

      // Assert
      const pullRequest = screen.getByRole('region', { name: 'Pull request' });
      expect((within(pullRequest).getByLabelText('Title') as HTMLInputElement).value).toBe(
        'Add a fake cart',
      );
      expect((within(pullRequest).getByLabelText('Description') as HTMLTextAreaElement).value).toBe(
        'Adds a **made-up** cart.',
      );
    });

    it('emits the whole content when the title changes', async () => {
      // Arrange
      const { contentChange } = await renderEditor();
      const pullRequest = screen.getByRole('region', { name: 'Pull request' });

      // Act
      fireEvent.input(within(pullRequest).getByLabelText('Title'), {
        target: { value: 'Add a pretend cart' },
      });

      // Assert
      expect(contentChange).toHaveBeenCalledWith(buildContent({ title: 'Add a pretend cart' }));
    });

    it('emits the whole content when the description changes', async () => {
      // Arrange
      const { contentChange } = await renderEditor();
      const pullRequest = screen.getByRole('region', { name: 'Pull request' });

      // Act
      fireEvent.input(within(pullRequest).getByLabelText('Description'), {
        target: { value: 'A fake description.' },
      });

      // Assert
      expect(contentChange).toHaveBeenCalledWith(
        buildContent({ description: 'A fake description.' }),
      );
    });

    it('fills in an empty content', async () => {
      // Arrange
      const { contentChange } = await renderEditor({ candidate: {}, staff: {} } as Content);

      // Act
      fireEvent.click(screen.getByRole('button', { name: 'Add changed file' }));

      // Assert
      expect(lastContent(contentChange)).toEqual({
        candidate: {
          title: '',
          description: '',
          files: [{ path: '', language: 'plaintext', original: '', modified: '' }],
        },
        staff: { expectedFindings: [] },
      });
    });
  });

  describe('changed files', () => {
    it('adds an empty file', async () => {
      // Arrange
      const { contentChange } = await renderEditor();

      // Act
      fireEvent.click(screen.getByRole('button', { name: 'Add changed file' }));

      // Assert
      expect(lastContent(contentChange).candidate.files).toEqual([
        fakeFile,
        { path: '', language: 'plaintext', original: '', modified: '' },
      ]);
    });

    it("can't add more files than the limit", async () => {
      // Act
      await renderEditor(buildContent({ files: Array.from({ length: 20 }, () => fakeFile) }), {});

      // Assert
      expect(
        (screen.getByRole('button', { name: 'Add changed file' }) as HTMLButtonElement).disabled,
      ).toBe(true);
    });

    it('removes a file', async () => {
      // Arrange
      const other = { ...fakeFile, path: 'src/fake-order.ts' };
      const { contentChange } = await renderEditor(buildContent({ files: [fakeFile, other] }));

      // Act
      fireEvent.click(screen.getByRole('button', { name: 'Remove changed file 1' }));

      // Assert
      expect(lastContent(contentChange).candidate.files).toEqual([other]);
    });

    it('renames a file and moves its expected findings along', async () => {
      // Arrange
      const { contentChange } = await renderEditor();

      // Act
      fireEvent.input(within(file(1)).getByLabelText('Path'), {
        target: { value: 'src/fake-basket.ts' },
      });

      // Assert
      expect(lastContent(contentChange)).toEqual(
        buildContent({ files: [{ ...fakeFile, path: 'src/fake-basket.ts' }] }, [
          { ...fakeFinding, file: 'src/fake-basket.ts' },
        ]),
      );
    });

    it('keeps the expected findings when the new path belongs to another file', async () => {
      // Arrange
      const other = { ...fakeFile, path: 'src/fake-order.ts' };
      const { contentChange } = await renderEditor(buildContent({ files: [fakeFile, other] }));

      // Act
      fireEvent.input(within(file(1)).getByLabelText('Path'), {
        target: { value: 'src/fake-order.ts' },
      });

      // Assert
      expect(lastContent(contentChange).staff.expectedFindings).toEqual([fakeFinding]);
    });

    it('picks a language from the closed list', async () => {
      // Arrange
      const { contentChange } = await renderEditor();
      const language = within(file(1)).getByLabelText('Language') as HTMLSelectElement;

      // Act
      fireEvent.change(language, { target: { value: 'csharp' } });

      // Assert
      expect(language.options).toHaveLength(11);
      expect(lastContent(contentChange).candidate.files[0].language).toBe('csharp');
    });

    it('edits the original and modified text in code editors', async () => {
      // Arrange
      const { monaco, contentChange } = await renderEditor();
      await vi.waitFor(() => expect(monaco.codeEditors).toHaveLength(2));
      const [original, modified] = monaco.codeEditors;

      // Act
      original.model?.setValue('const total = 3;');
      const afterOriginal = lastContent(contentChange);
      modified.model?.setValue('const total = 4;');

      // Assert
      expect(original.createOptions['ariaLabel']).toBe('Original text of changed file 1');
      expect(original.model?.language).toBe('typescript');
      expect(afterOriginal.candidate.files[0].original).toBe('const total = 3;');
      expect(lastContent(contentChange).candidate.files[0].modified).toBe('const total = 4;');
    });

    it('switches between the original and modified side', async () => {
      // Arrange
      const { container } = await renderEditor();
      const editors = container.querySelectorAll('hl-code-editor');

      // Act
      fireEvent.click(within(file(1)).getByRole('button', { name: 'Original' }));

      // Assert
      expect(
        within(file(1)).getByRole('button', { name: 'Original' }).getAttribute('aria-pressed'),
      ).toBe('true');
      expect(
        within(file(1)).getByRole('button', { name: 'Modified' }).getAttribute('aria-pressed'),
      ).toBe('false');
      expect(editors[0].hasAttribute('hidden')).toBe(false);
      expect(editors[1].hasAttribute('hidden')).toBe(true);
    });

    it('marks a file as added, without an original', async () => {
      // Arrange
      const { contentChange } = await renderEditor();

      // Act
      fireEvent.change(within(file(1)).getByLabelText('Change'), { target: { value: 'added' } });

      // Assert
      expect(lastContent(contentChange).candidate.files[0]).toEqual({
        ...fakeFile,
        original: null,
      });
    });

    it('shows only the modified side of an added file and can turn it back', async () => {
      // Arrange
      const { container, contentChange } = await renderEditor(
        buildContent({ files: [{ ...fakeFile, original: null }] }),
      );

      // Act
      fireEvent.change(within(file(1)).getByLabelText('Change'), {
        target: { value: 'modified' },
      });

      // Assert
      expect(container.querySelectorAll('hl-code-editor')).toHaveLength(1);
      expect(within(file(1)).queryByRole('button', { name: 'Original' })).toBeNull();
      expect(lastContent(contentChange).candidate.files[0]).toEqual({ ...fakeFile, original: '' });
    });
  });

  describe('expected findings', () => {
    it('adds a finding on the first file with an unused id', async () => {
      // Arrange
      const { contentChange } = await renderEditor(
        buildContent({}, [{ ...fakeFinding, id: 'finding-2' }]),
      );

      // Act
      fireEvent.click(screen.getByRole('button', { name: 'Add expected finding' }));

      // Assert
      expect(lastContent(contentChange).staff.expectedFindings[1]).toEqual({
        id: 'finding-3',
        file: 'src/fake-cart.ts',
        side: 'modified',
        startLine: 1,
        endLine: 1,
        title: '',
        description: '',
        severity: 'Major',
      });
    });

    it('removes a finding', async () => {
      // Arrange
      const { contentChange } = await renderEditor();

      // Act
      fireEvent.click(screen.getByRole('button', { name: 'Remove expected finding 1' }));

      // Assert
      expect(lastContent(contentChange)).toEqual(buildContent({}, []));
    });

    it('shows the finding', async () => {
      // Act
      await renderEditor();

      // Assert
      const fields = within(finding(1));
      expect((fields.getByLabelText('Title') as HTMLInputElement).value).toBe('Wrong total');
      expect((fields.getByLabelText('Id') as HTMLInputElement).value).toBe('wrong-total');
      expect((fields.getByLabelText('File') as HTMLSelectElement).value).toBe('src/fake-cart.ts');
      expect((fields.getByLabelText('Side') as HTMLSelectElement).value).toBe('modified');
      expect((fields.getByLabelText('Start line') as HTMLInputElement).value).toBe('1');
      expect((fields.getByLabelText('End line') as HTMLInputElement).value).toBe('1');
      expect((fields.getByLabelText('Severity') as HTMLSelectElement).value).toBe('Major');
      expect((fields.getByLabelText('Description') as HTMLTextAreaElement).value).toBe(
        'The total is made up.',
      );
    });

    it.each([
      ['Title', 'input', 'Total is pretend', { title: 'Total is pretend' }],
      ['Id', 'input', 'pretend-total', { id: 'pretend-total' }],
      ['Description', 'input', 'Pretend.', { description: 'Pretend.' }],
      ['Side', 'change', 'original', { side: 'original' }],
      ['Severity', 'change', 'Nit', { severity: 'Nit' }],
      ['Start line', 'change', '2', { startLine: 2 }],
      ['End line', 'change', '3', { endLine: 3 }],
    ] as const)('edits the %s', async (label, event, value, change) => {
      // Arrange
      const { contentChange } = await renderEditor();

      // Act
      fireEvent[event](within(finding(1)).getByLabelText(label), { target: { value } });

      // Assert
      expect(lastContent(contentChange)).toEqual(buildContent({}, [{ ...fakeFinding, ...change }]));
    });

    it('picks the file from the paths of the changed files', async () => {
      // Arrange
      const other = { ...fakeFile, path: 'src/fake-order.ts' };
      const { contentChange } = await renderEditor(
        buildContent({ files: [fakeFile, other, { ...fakeFile, path: '' }] }),
      );
      const select = within(finding(1)).getByLabelText('File') as HTMLSelectElement;

      // Act
      fireEvent.change(select, { target: { value: 'src/fake-order.ts' } });

      // Assert
      expect([...select.options].map((option) => option.value)).toEqual([
        'src/fake-cart.ts',
        'src/fake-order.ts',
      ]);
      expect(lastContent(contentChange).staff.expectedFindings[0].file).toBe('src/fake-order.ts');
    });

    it('keeps a file that is no longer a changed file as an option', async () => {
      // Act
      await renderEditor(buildContent({}, [{ ...fakeFinding, file: 'src/gone.ts' }]));

      // Assert
      const select = within(finding(1)).getByLabelText('File') as HTMLSelectElement;
      expect(select.value).toBe('src/gone.ts');
    });

    it('stores an empty line number as 0', async () => {
      // Arrange
      const { contentChange } = await renderEditor();

      // Act
      fireEvent.change(within(finding(1)).getByLabelText('Start line'), { target: { value: '' } });

      // Assert
      expect(lastContent(contentChange).staff.expectedFindings[0].startLine).toBe(0);
    });
  });

  describe('validation errors', () => {
    function expectError(field: HTMLElement, message: string) {
      const error = screen.getByText(message);
      expect(field.getAttribute('data-matches-spartan-invalid')).toBe('true');
      expect(field.getAttribute('aria-describedby')).toContain(error.closest('ul')?.id);
    }

    it('shows the errors of the pull request fields', async () => {
      // Act
      await renderEditor(buildContent(), {
        'candidateContent.title': ['Enter a title.'],
        'candidateContent.description': ['Keep the description short.'],
      });

      // Assert
      const pullRequest = within(screen.getByRole('region', { name: 'Pull request' }));
      expectError(pullRequest.getByLabelText('Title'), 'Enter a title.');
      expectError(pullRequest.getByLabelText('Description'), 'Keep the description short.');
    });

    it('shows the errors of the content and of the lists', async () => {
      // Act
      await renderEditor(buildContent({ files: [] }, []), {
        candidateContent: ['Fake content error.'],
        'candidateContent.files': ['Add 1 to 20 changed files.'],
        'staffContent.expectedFindings': ['Add at most 50 expected findings.'],
      });

      // Assert
      expect(screen.getByText('Fake content error.')).toBeTruthy();
      expect(
        within(screen.getByRole('region', { name: 'Changed files' })).getByText(
          'Add 1 to 20 changed files.',
        ),
      ).toBeTruthy();
      expect(
        within(screen.getByRole('region', { name: 'Expected findings' })).getByText(
          'Add at most 50 expected findings.',
        ),
      ).toBeTruthy();
    });

    it('shows indexed errors next to the fields of the matching file', async () => {
      // Arrange
      const other = { ...fakeFile, path: 'src/fake-order.ts' };

      // Act
      await renderEditor(buildContent({ files: [fakeFile, other] }), {
        'candidateContent.files[1].path': ['Another changed file already uses this path.'],
        'candidateContent.files[1].language': ['Choose one of the languages.'],
        'candidateContent.files[1]': ['Enter the original text, the modified text or both.'],
        'candidateContent.files[1].modified': ['Keep the text to at most 200 KB.'],
      });

      // Assert
      expect(within(file(1)).queryByRole('list')).toBeNull();
      expectError(
        within(file(2)).getByLabelText('Path'),
        'Another changed file already uses this path.',
      );
      expectError(within(file(2)).getByLabelText('Language'), 'Choose one of the languages.');
      expect(
        within(file(2)).getByText('Enter the original text, the modified text or both.'),
      ).toBeTruthy();
      expect(within(file(2)).getByText('Keep the text to at most 200 KB.')).toBeTruthy();
    });

    it('shows indexed errors next to the fields of the matching finding', async () => {
      // Act
      await renderEditor(buildContent({}, [fakeFinding, { ...fakeFinding, id: 'second' }]), {
        'staffContent.expectedFindings[1]': ['Define the expected finding.'],
        'staffContent.expectedFindings[1].id': ['Use a kebab-case id.'],
        'staffContent.expectedFindings[1].file': ['Choose the path of one of the changed files.'],
        'staffContent.expectedFindings[1].side': ['The file has no original text.'],
        'staffContent.expectedFindings[1].startLine': ['Start at line 1 or later.'],
        'staffContent.expectedFindings[1].endLine': ['The modified side has 1 lines.'],
        'staffContent.expectedFindings[1].title': ['Enter a title.'],
        'staffContent.expectedFindings[1].description': ['Keep the description short.'],
        'staffContent.expectedFindings[1].severity': ['Choose Critical, Major, Minor or Nit.'],
      });

      // Assert
      const fields = within(finding(2));
      expect(within(finding(1)).queryByRole('list')).toBeNull();
      expect(fields.getByText('Define the expected finding.')).toBeTruthy();
      expectError(fields.getByLabelText('Id'), 'Use a kebab-case id.');
      expectError(fields.getByLabelText('File'), 'Choose the path of one of the changed files.');
      expectError(fields.getByLabelText('Side'), 'The file has no original text.');
      expectError(fields.getByLabelText('Start line'), 'Start at line 1 or later.');
      expectError(fields.getByLabelText('End line'), 'The modified side has 1 lines.');
      expectError(fields.getByLabelText('Title'), 'Enter a title.');
      expectError(fields.getByLabelText('Description'), 'Keep the description short.');
      expectError(fields.getByLabelText('Severity'), 'Choose Critical, Major, Minor or Nit.');
    });

    it('is valid without errors', async () => {
      // Act
      await renderEditor();

      // Assert
      expect(screen.queryByRole('list')).toBeNull();
      expect(
        within(finding(1)).getByLabelText('Title').getAttribute('data-matches-spartan-invalid'),
      ).toBeNull();
    });
  });
});
