import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './auth/auth.guards';
import { designRoutes } from './design/design.routes';
import { AppShell } from './shell/app-shell';
import { shellRoutes } from './shell/shell.routes';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Log in - HireLab',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'invite',
    title: 'Accept your invite - HireLab',
    loadComponent: () => import('./invites/invite-page').then((m) => m.InvitePage),
  },
  {
    path: 'reset',
    title: 'Set a new password - HireLab',
    loadComponent: () => import('./reset-links/reset-page').then((m) => m.ResetPage),
  },
  ...designRoutes,
  {
    path: '',
    component: AppShell,
    canActivate: [authGuard],
    canActivateChild: [roleGuard],
    children: shellRoutes,
  },
  { path: '**', redirectTo: '' },
];
