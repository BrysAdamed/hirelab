import { inject } from '@angular/core';
import { type CanActivateFn, RedirectCommand, Router } from '@angular/router';
import { InterviewService } from './interview.service';

/**
 * Opens a candidate link `/s/{token}` (ADR 0008): hands the token to {@link InterviewService} and replaces the address
 * with `/s/`, so the token leaves the address bar and the history before it is exchanged.
 */
export const openLink: CanActivateFn = (route) => {
  inject(InterviewService).holdToken(route.paramMap.get('token') ?? '');
  return new RedirectCommand(inject(Router).parseUrl('/'), { replaceUrl: true });
};
