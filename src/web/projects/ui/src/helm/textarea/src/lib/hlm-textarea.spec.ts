import { render, screen } from '@testing-library/angular';
import { HlmTextarea } from './hlm-textarea';

describe('HlmTextarea', () => {
  it('links a static hint through aria-describedby', async () => {
    // Arrange
    await render(
      `<label for="summary">Summary</label>
       <textarea hlmTextarea id="summary" aria-describedby="summary-hint"></textarea>
       <p id="summary-hint">Markdown is supported.</p>`,
      { imports: [HlmTextarea] },
    );

    // Act
    const textarea = screen.getByLabelText('Summary');

    // Assert
    expect(textarea.getAttribute('aria-describedby')).toBe('summary-hint');
  });

  it('links a bound hint through aria-describedby', async () => {
    // Arrange
    await render(
      `<label for="summary">Summary</label>
       <textarea hlmTextarea id="summary" [aria-describedby]="hintId"></textarea>
       <p [id]="hintId">Markdown is supported.</p>`,
      { imports: [HlmTextarea], componentProperties: { hintId: 'summary-hint' } },
    );

    // Act
    const textarea = screen.getByLabelText('Summary');

    // Assert
    expect(textarea.getAttribute('aria-describedby')).toBe('summary-hint');
  });
});
