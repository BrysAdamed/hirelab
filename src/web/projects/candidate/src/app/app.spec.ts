import { render, screen } from '@testing-library/angular';
import { App } from './app';

describe('App', () => {
  it('renders the shell with the product name and a main region', async () => {
    // Act
    await render(App);

    // Assert
    expect(screen.getByRole('banner').textContent).toContain('HireLab');
    expect(screen.getByRole('main')).toBeTruthy();
  });
});
