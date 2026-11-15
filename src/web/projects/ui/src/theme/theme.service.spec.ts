import { DOCUMENT } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { CONTRAST_STORAGE_KEY, THEME_STORAGE_KEY, type Theme, ThemeService } from './theme.service';

interface FakeMediaQueryList {
  matches: boolean;
  addEventListener: (type: 'change', listener: (event: { matches: boolean }) => void) => void;
  removeEventListener: () => void;
  change: (matches: boolean) => void;
}

function fakeMediaQuery(matches: boolean): FakeMediaQueryList {
  const listeners: ((event: { matches: boolean }) => void)[] = [];
  return {
    matches,
    addEventListener: (_type, listener) => listeners.push(listener),
    removeEventListener: () => undefined,
    change(value) {
      this.matches = value;
      listeners.forEach((listener) => listener({ matches: value }));
    },
  };
}

describe('ThemeService', () => {
  let prefersDark: FakeMediaQueryList;
  let prefersMoreContrast: FakeMediaQueryList;

  function setup(system: { dark?: boolean; moreContrast?: boolean } = {}): ThemeService {
    prefersDark = fakeMediaQuery(system.dark ?? false);
    prefersMoreContrast = fakeMediaQuery(system.moreContrast ?? false);
    vi.spyOn(window, 'matchMedia').mockImplementation(
      (query: string) =>
        (query === '(prefers-color-scheme: dark)'
          ? prefersDark
          : prefersMoreContrast) as unknown as MediaQueryList,
    );
    const service = TestBed.inject(ThemeService);
    TestBed.tick();
    return service;
  }

  function htmlThemeClasses(): string[] {
    const themes: Theme[] = ['light', 'dark', 'hc-light', 'hc-dark'];
    const classList = TestBed.inject(DOCUMENT).documentElement.classList;
    return themes.filter((theme) => classList.contains(theme));
  }

  beforeEach(() => {
    if (!window.matchMedia) {
      window.matchMedia = () => fakeMediaQuery(false) as unknown as MediaQueryList;
    }
    localStorage.clear();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('follows the system by default', () => {
    // Act
    const service = setup({ dark: true, moreContrast: true });

    // Assert
    expect(service.themePreference()).toBe('system');
    expect(service.contrastPreference()).toBe('system');
    expect(service.theme()).toBe('hc-dark');
    expect(htmlThemeClasses()).toEqual(['hc-dark']);
  });

  it('reacts when the system setting changes', () => {
    // Arrange
    const service = setup();

    // Act
    prefersDark.change(true);
    TestBed.tick();

    // Assert
    expect(service.theme()).toBe('dark');
    expect(htmlThemeClasses()).toEqual(['dark']);
  });

  it('lets the user override the system and saves the choice', () => {
    // Arrange
    const service = setup({ dark: true });

    // Act
    service.setThemePreference('light');
    service.setContrastPreference('on');
    TestBed.tick();

    // Assert
    expect(service.theme()).toBe('hc-light');
    expect(htmlThemeClasses()).toEqual(['hc-light']);
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('light');
    expect(localStorage.getItem(CONTRAST_STORAGE_KEY)).toBe('on');
  });

  it('restores saved preferences', () => {
    // Arrange
    localStorage.setItem(THEME_STORAGE_KEY, 'dark');
    localStorage.setItem(CONTRAST_STORAGE_KEY, 'off');

    // Act
    const service = setup({ moreContrast: true });

    // Assert
    expect(service.theme()).toBe('dark');
  });

  it('ignores unknown saved values', () => {
    // Arrange
    localStorage.setItem(THEME_STORAGE_KEY, 'sepia');

    // Act
    const service = setup();

    // Assert
    expect(service.themePreference()).toBe('system');
    expect(service.theme()).toBe('light');
  });
});
