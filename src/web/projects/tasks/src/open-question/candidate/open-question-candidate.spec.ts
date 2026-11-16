import { candidateTaskTypes } from '@hirelab/tasks/candidate';
import { fireEvent, render, screen } from '@testing-library/angular';
import type { OpenQuestionResponse } from './open-question';
import { OpenQuestionCandidate } from './open-question-candidate';

describe('OpenQuestionCandidate', () => {
  const content = { prompt: '## Fake question\n\nExplain a **made-up** pattern.' };

  async function renderCandidate(response: OpenQuestionResponse | null = null) {
    const responseChange = vi.fn<(response: OpenQuestionResponse) => void>();
    const view = await render(OpenQuestionCandidate, {
      inputs: { content, response },
      on: { responseChange },
    });
    return { ...view, responseChange };
  }

  it('is the candidate entry point of the open question type', async () => {
    // Act
    const component = await candidateTaskTypes.find('open-question')?.loadComponent();

    // Assert
    expect(component).toBe(OpenQuestionCandidate);
  });

  it('renders the prompt as Markdown', async () => {
    // Act
    await renderCandidate();

    // Assert
    expect(screen.getByRole('heading', { name: 'Fake question' })).toBeTruthy();
    expect(screen.getByText('made-up').tagName).toBe('STRONG');
  });

  it('starts with an empty answer when there is no response yet', async () => {
    // Act
    await renderCandidate();

    // Assert
    expect((screen.getByLabelText('Your answer') as HTMLTextAreaElement).value).toBe('');
    expect(screen.getByText(/0 of\s+20,000 characters/)).toBeTruthy();
  });

  it('shows the saved answer', async () => {
    // Act
    await renderCandidate({ answer: 'A fake answer' });

    // Assert
    expect((screen.getByLabelText('Your answer') as HTMLTextAreaElement).value).toBe(
      'A fake answer',
    );
    expect(screen.getByText(/13 of\s+20,000 characters/)).toBeTruthy();
  });

  it('emits the whole response on every change', async () => {
    // Arrange
    const { responseChange } = await renderCandidate({ answer: 'A fake' });

    // Act
    fireEvent.input(screen.getByLabelText('Your answer'), {
      target: { value: 'A fake answer' },
    });

    // Assert
    expect(responseChange).toHaveBeenCalledWith({ answer: 'A fake answer' });
  });

  it('limits the answer to 20,000 characters', async () => {
    // Act
    await renderCandidate();

    // Assert
    const answer = screen.getByLabelText('Your answer');
    expect(answer.getAttribute('maxlength')).toBe('20000');
    expect(answer.getAttribute('aria-describedby')).toBe(
      screen.getByText(/You can use Markdown/).id,
    );
  });

  it('previews the answer as Markdown and switches back to writing', async () => {
    // Arrange
    const { fixture } = await renderCandidate({
      answer: '- first fake point\n- second fake point',
    });

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Preview' }));
    fixture.detectChanges();

    // Assert
    expect(screen.queryByLabelText('Your answer')).toBeNull();
    expect(screen.getAllByRole('listitem').map((item) => item.textContent)).toEqual([
      'first fake point',
      'second fake point',
    ]);
    expect(screen.getByRole('button', { name: 'Preview' }).getAttribute('aria-pressed')).toBe(
      'true',
    );

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Write' }));
    fixture.detectChanges();

    // Assert
    expect((screen.getByLabelText('Your answer') as HTMLTextAreaElement).value).toBe(
      '- first fake point\n- second fake point',
    );
  });

  it('says there is nothing to preview for an empty answer', async () => {
    // Arrange
    const { fixture } = await renderCandidate({ answer: '  ' });

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Preview' }));
    fixture.detectChanges();

    // Assert
    expect(screen.getByText('Nothing to preview yet.')).toBeTruthy();
  });
});
