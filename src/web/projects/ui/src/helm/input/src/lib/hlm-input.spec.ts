import { render, screen } from '@testing-library/angular';
import { HlmInput } from './hlm-input';

describe('HlmInput', () => {
  it('links a static hint through aria-describedby', async () => {
    // Arrange
    await render(
      `<label for="nickname">Nickname</label>
       <input hlmInput id="nickname" aria-describedby="nickname-hint" />
       <p id="nickname-hint">Shown to other interviewers.</p>`,
      { imports: [HlmInput] },
    );

    // Act
    const input = screen.getByLabelText('Nickname');

    // Assert
    expect(input.getAttribute('aria-describedby')).toBe('nickname-hint');
  });

  it('links a bound hint through aria-describedby', async () => {
    // Arrange
    await render(
      `<label for="nickname">Nickname</label>
       <input hlmInput id="nickname" [aria-describedby]="hintId" />
       <p [id]="hintId">Shown to other interviewers.</p>`,
      { imports: [HlmInput], componentProperties: { hintId: 'nickname-hint' } },
    );

    // Act
    const input = screen.getByLabelText('Nickname');

    // Assert
    expect(input.getAttribute('aria-describedby')).toBe('nickname-hint');
  });
});
