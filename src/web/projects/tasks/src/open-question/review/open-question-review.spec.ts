import { reviewTaskTypes } from '@hirelab/tasks/review';
import { render, screen } from '@testing-library/angular';
import type { OpenQuestionResponse } from '../candidate/open-question';
import { OpenQuestionReview } from './open-question-review';

describe('OpenQuestionReview', () => {
  const content = { candidate: { prompt: 'Explain a **made-up** pattern.' }, staff: {} };

  function renderReview(
    response: OpenQuestionResponse | null,
    options: { live?: boolean; interviewerNotes?: string } = {},
  ) {
    return render(OpenQuestionReview, {
      inputs: {
        content,
        response,
        referenceAnswer: 'A strong answer names the *fake* benefits.',
        interviewerNotes: options.interviewerNotes,
        live: options.live ?? false,
      },
    });
  }

  it('is the review entry point of the open question type', async () => {
    // Act
    const component = await reviewTaskTypes.find('open-question')?.loadComponent();

    // Assert
    expect(component).toBe(OpenQuestionReview);
  });

  it('renders the prompt, the answer and the reference answer as Markdown', async () => {
    // Act
    await renderReview({ answer: '## Fake heading\n\nA **bold fake** answer.' });

    // Assert
    expect(screen.getByText('made-up').tagName).toBe('STRONG');
    expect(screen.getByRole('heading', { name: 'Fake heading' })).toBeTruthy();
    expect(screen.getByText('bold fake').tagName).toBe('STRONG');
    expect(screen.getByText('fake', { selector: 'em' })).toBeTruthy();
  });

  it('shows the interviewer notes when the task has them', async () => {
    // Act
    await renderReview(
      { answer: 'A fake answer.' },
      { interviewerNotes: 'Ask a made-up follow-up.' },
    );

    // Assert
    expect(screen.getByRole('heading', { name: 'Interviewer notes' })).toBeTruthy();
    expect(screen.getByText('Ask a made-up follow-up.')).toBeTruthy();
  });

  it('leaves the interviewer notes out when there are none', async () => {
    // Act
    await renderReview({ answer: 'A fake answer.' });

    // Assert
    expect(screen.queryByRole('heading', { name: 'Interviewer notes' })).toBeNull();
  });

  it('says the candidate has not written anything yet while the session is live', async () => {
    // Act
    await renderReview(null, { live: true });

    // Assert
    expect(screen.getByText('The candidate hasn’t written anything yet.')).toBeTruthy();
  });

  it('says there is no answer once the session is over', async () => {
    // Act
    await renderReview({ answer: ' ' });

    // Assert
    expect(screen.getByText('No answer.')).toBeTruthy();
  });
});
