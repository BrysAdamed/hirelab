import { fireEvent, render, screen } from '@testing-library/angular';
import { CopyButton } from './copy-button';

describe('CopyButton', () => {
  function useClipboard(writeText: Clipboard['writeText']): void {
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });
  }

  afterEach(() => Reflect.deleteProperty(navigator, 'clipboard'));

  it('copies the text and confirms it', async () => {
    // Arrange
    const writeText = vi.fn().mockResolvedValue(undefined);
    useClipboard(writeText);
    await render(CopyButton, {
      inputs: { text: 'https://example.com/invite#fake', label: 'Copy link' },
    });

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Copy link' }));

    // Assert
    expect(await screen.findByText('Copied')).toBeTruthy();
    expect(writeText).toHaveBeenCalledWith('https://example.com/invite#fake');
  });

  it('asks the user to copy by hand when the clipboard refuses', async () => {
    // Arrange
    const writeText = vi.fn().mockRejectedValue(new Error('Denied'));
    useClipboard(writeText);
    await render(CopyButton, { inputs: { text: 'fake' } });

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Copy' }));

    // Assert
    expect(await screen.findByText(/Copying failed/)).toBeTruthy();
  });
});
