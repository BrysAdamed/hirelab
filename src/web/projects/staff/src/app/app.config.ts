import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideTheme } from '@hirelab/ui';
import { provideSpartanHlm } from '@hirelab/ui/helm/utils';
import { routes } from './app.routes';
import { provideAuth } from './auth/provide-auth';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideSpartanHlm(),
    provideTheme(),
    provideAuth(),
  ],
};
