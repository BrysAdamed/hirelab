import { TestBed } from '@angular/core/testing';
import { render, screen, within } from '@testing-library/angular';
import { ThemeSwitcher } from './theme-switcher';
import { ThemeService } from './theme.service';

describe('ThemeSwitcher', () => {
  afterEach(() => localStorage.clear());

  it('switches the theme and marks the chosen option as pressed', async () => {
    // Arrange
    await render(ThemeSwitcher);
    const themeGroup = screen.getByRole('group', { name: 'Theme' });

    // Act
    within(themeGroup).getByRole('button', { name: 'Dark' }).click();
    TestBed.tick();

    // Assert
    expect(TestBed.inject(ThemeService).theme()).toBe('dark');
    expect(within(themeGroup).getByRole('button', { name: 'Dark' }).ariaPressed).toBe('true');
    expect(within(themeGroup).getByRole('button', { name: 'System' }).ariaPressed).toBe('false');
  });

  it('turns high contrast on independently of the theme', async () => {
    // Arrange
    await render(ThemeSwitcher);
    const contrastGroup = screen.getByRole('group', { name: 'High contrast' });

    // Act
    within(contrastGroup).getByRole('button', { name: 'On' }).click();
    TestBed.tick();

    // Assert
    expect(TestBed.inject(ThemeService).contrastPreference()).toBe('on');
    expect(TestBed.inject(ThemeService).theme()).toBe('hc-light');
  });
});
