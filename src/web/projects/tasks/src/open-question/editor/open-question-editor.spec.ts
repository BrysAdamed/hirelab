import { editorTaskTypes, type TaskContentErrors } from '@hirelab/tasks/editor';
import { fireEvent, render, screen } from '@testing-library/angular';
import { OpenQuestionEditor } from './open-question-editor';

describe('OpenQuestionEditor', () => {
  async function renderEditor(prompt: string, errors: TaskContentErrors = {}) {
    const contentChange = vi.fn();
    const view = await render(OpenQuestionEditor, {
      inputs: { content: { candidate: { prompt }, staff: {} }, errors },
      on: { contentChange },
    });
    return { ...view, contentChange };
  }

  it('is the editor entry point of the open question type', async () => {
    // Act
    const component = await editorTaskTypes.find('open-question')?.loadComponent();

    // Assert
    expect(component).toBe(OpenQuestionEditor);
  });

  it('shows the prompt', async () => {
    // Act
    await renderEditor('Explain a made-up pattern.');

    // Assert
    expect((screen.getByLabelText('Prompt') as HTMLTextAreaElement).value).toBe(
      'Explain a made-up pattern.',
    );
  });

  it('emits the whole content with empty staff content when the prompt changes', async () => {
    // Arrange
    const { contentChange } = await renderEditor('Explain');

    // Act
    fireEvent.input(screen.getByLabelText('Prompt'), {
      target: { value: 'Explain a fake idea.' },
    });

    // Assert
    expect(contentChange).toHaveBeenCalledWith({
      candidate: { prompt: 'Explain a fake idea.' },
      staff: {},
    });
  });

  it('shows the backend validation errors of the prompt', async () => {
    // Act
    await renderEditor(' ', { 'candidateContent.prompt': ['Enter a prompt.'] });

    // Assert
    const prompt = screen.getByLabelText('Prompt');
    expect(prompt.getAttribute('data-matches-spartan-invalid')).toBe('true');
    expect(screen.getByText('Enter a prompt.')).toBeTruthy();
    expect(prompt.getAttribute('aria-describedby')).toContain(
      screen.getByText('Enter a prompt.').closest('ul')?.id,
    );
  });

  it('is valid without errors', async () => {
    // Act
    await renderEditor('Explain a made-up pattern.');

    // Assert
    expect(screen.getByLabelText('Prompt').getAttribute('data-matches-spartan-invalid')).toBeNull();
    expect(screen.queryByRole('list')).toBeNull();
  });
});
