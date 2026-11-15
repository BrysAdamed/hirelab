import { type EnvironmentProviders, inject, provideAppInitializer } from '@angular/core';
import { ThemeService } from './theme.service';

/** Applies the saved or system theme to `<html>` when the app starts. */
export function provideTheme(): EnvironmentProviders {
  return provideAppInitializer(() => {
    inject(ThemeService);
  });
}
