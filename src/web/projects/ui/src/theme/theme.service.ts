import {
  DOCUMENT,
  DestroyRef,
  Injectable,
  type Signal,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';

/** Light or dark, chosen by the user or taken from the system. */
export type ThemePreference = 'system' | 'light' | 'dark';

/** High contrast on or off, chosen by the user or taken from `prefers-contrast: more`. */
export type ContrastPreference = 'system' | 'on' | 'off';

/** One of the four token sets defined in `styles/theme.css`. */
export type Theme = 'light' | 'dark' | 'hc-light' | 'hc-dark';

export const THEMES: readonly Theme[] = ['light', 'dark', 'hc-light', 'hc-dark'];

const themePreferences: readonly ThemePreference[] = ['system', 'light', 'dark'];
const contrastPreferences: readonly ContrastPreference[] = ['system', 'on', 'off'];

export const THEME_STORAGE_KEY = 'hirelab.theme';
export const CONTRAST_STORAGE_KEY = 'hirelab.contrast';

/**
 * Resolves the active theme from the user's preferences and the system settings,
 * persists the preferences in `localStorage` and applies the theme class to `<html>`.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);
  private readonly window = this.document.defaultView;

  private readonly systemPrefersDark = this.mediaQuery('(prefers-color-scheme: dark)');
  private readonly systemPrefersMoreContrast = this.mediaQuery('(prefers-contrast: more)');

  private readonly _themePreference = signal(
    this.read(THEME_STORAGE_KEY, themePreferences, 'system'),
  );
  private readonly _contrastPreference = signal(
    this.read(CONTRAST_STORAGE_KEY, contrastPreferences, 'system'),
  );

  readonly themePreference = this._themePreference.asReadonly();
  readonly contrastPreference = this._contrastPreference.asReadonly();

  readonly theme = computed<Theme>(() => {
    const themePreference = this._themePreference();
    const contrastPreference = this._contrastPreference();
    const dark =
      themePreference === 'system' ? this.systemPrefersDark() : themePreference === 'dark';
    const highContrast =
      contrastPreference === 'system'
        ? this.systemPrefersMoreContrast()
        : contrastPreference === 'on';
    const base = dark ? 'dark' : 'light';
    return highContrast ? `hc-${base}` : base;
  });

  constructor() {
    effect(() => this.apply(this.theme()));
  }

  setThemePreference(preference: ThemePreference): void {
    this._themePreference.set(preference);
    this.write(THEME_STORAGE_KEY, preference);
  }

  setContrastPreference(preference: ContrastPreference): void {
    this._contrastPreference.set(preference);
    this.write(CONTRAST_STORAGE_KEY, preference);
  }

  private apply(theme: Theme): void {
    const classList = this.document.documentElement.classList;
    classList.remove(...THEMES);
    classList.add(theme);
  }

  private mediaQuery(query: string): Signal<boolean> {
    const mediaQueryList = this.window?.matchMedia?.(query);
    const matches = signal(mediaQueryList?.matches ?? false);
    if (mediaQueryList) {
      const listener = (event: MediaQueryListEvent) => matches.set(event.matches);
      mediaQueryList.addEventListener('change', listener);
      this.destroyRef.onDestroy(() => mediaQueryList.removeEventListener('change', listener));
    }
    return matches.asReadonly();
  }

  private read<T extends string>(key: string, allowed: readonly T[], fallback: T): T {
    try {
      const value = this.window?.localStorage.getItem(key);
      return allowed.find((option) => option === value) ?? fallback;
    } catch {
      return fallback;
    }
  }

  private write(key: string, value: string): void {
    try {
      this.window?.localStorage.setItem(key, value);
    } catch {
      return;
    }
  }
}
