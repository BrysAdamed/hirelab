import { render, screen } from '@testing-library/angular';
import { MarkdownViewer } from './markdown-viewer';

describe('MarkdownViewer', () => {
  it('renders Markdown as HTML', async () => {
    // Act
    await render(MarkdownViewer, { inputs: { markdown: '## Skills\n\n- **Fake** testing\n- Made-up tools' } });

    // Assert
    expect(screen.getByRole('heading', { name: 'Skills' })).toBeTruthy();
    expect(screen.getAllByRole('listitem').map((item) => item.textContent)).toEqual([
      'Fake testing',
      'Made-up tools',
    ]);
  });

  it('removes scripts, event handlers and javascript links', async () => {
    // Act
    const { container } = await render(MarkdownViewer, {
      inputs: {
        markdown:
          '<script>alert(1)</script><img src="x.png" onerror="alert(2)">\n\n[click](javascript:alert(3))',
      },
    });

    // Assert
    expect(container.querySelector('script')).toBeNull();
    expect(container.querySelector('img')?.getAttribute('onerror')).toBeNull();
    expect(screen.getByText('click').getAttribute('href')).toBeNull();
  });
});
