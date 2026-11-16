import {
  type EnvironmentProviders,
  Injector,
  inject,
  makeEnvironmentProviders,
} from '@angular/core';
import { SESSION_EXPIRED_HANDLER } from '../api/staff-api';
import { AuthService } from './auth.service';

/** Sends the user to login whenever a staff API call finds the session has ended. */
export function provideAuth(): EnvironmentProviders {
  return makeEnvironmentProviders([
    {
      provide: SESSION_EXPIRED_HANDLER,
      useFactory: () => {
        const injector = inject(Injector);
        return () => injector.get(AuthService).sessionExpired();
      },
    },
  ]);
}
