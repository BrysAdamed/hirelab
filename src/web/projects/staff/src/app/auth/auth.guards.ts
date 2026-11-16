import { inject } from '@angular/core';
import { type CanActivateChildFn, type CanActivateFn, Router } from '@angular/router';
import { staffPageOf } from '../shell/staff-page';
import { AuthService } from './auth.service';

/** Lets signed-in users through and sends everyone else to login, remembering where they were going. */
export const authGuard: CanActivateFn = async (_route, state) => {
  const router = inject(Router);
  const user = await inject(AuthService).ensureLoaded();
  return user ? true : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** Sends a signed-in user who opens the login page to the home page instead. */
export const guestGuard: CanActivateFn = async () => {
  const router = inject(Router);
  const user = await inject(AuthService).ensureLoaded();
  return user ? router.createUrlTree(['/']) : true;
};

/** Checks the roles a page declares with `staffPage(...)`; a user without one goes to the home page. */
export const roleGuard: CanActivateChildFn = (childRoute) => {
  const page = staffPageOf(childRoute.routeConfig);
  const role = inject(AuthService).user()?.role;
  if (!page || (role && page.roles.includes(role))) {
    return true;
  }
  return inject(Router).createUrlTree(['/']);
};

/** Accepts only a path inside the app as a return URL, so a crafted link can't send the user elsewhere. */
export function safeReturnUrl(returnUrl: string | null | undefined): string {
  if (!returnUrl || !returnUrl.startsWith('/') || returnUrl.startsWith('//')) {
    return '/';
  }
  return returnUrl.startsWith('/login') ? '/' : returnUrl;
}
