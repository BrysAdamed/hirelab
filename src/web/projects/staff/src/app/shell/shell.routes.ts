import type { Routes } from '@angular/router';
import {
  lucideBriefcase,
  lucideHouse,
  lucideLibraryBig,
  lucideUsers,
  lucideUsersRound,
} from '@ng-icons/lucide';
import { ALL_ROLES, staffPage } from './staff-page';

const placeholder = () => import('../pages/placeholder-page').then((m) => m.PlaceholderPage);

/**
 * The pages inside the app shell. The navigation is built from this list: a page with `nav` appears
 * for the roles it allows, in this order. Replace a placeholder's `loadComponent` when its feature lands.
 */
export const shellRoutes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    title: 'Home - HireLab',
    loadComponent: () => import('../pages/home-page').then((m) => m.HomePage),
    data: staffPage({ roles: ALL_ROLES, nav: { label: 'Home', icon: lucideHouse } }),
  },
  {
    path: 'positions',
    title: 'Job positions - HireLab',
    loadComponent: () => import('../positions/positions-page').then((m) => m.PositionsPage),
    data: staffPage({
      roles: ['Manager', 'Interviewer'],
      nav: { label: 'Job positions', icon: lucideBriefcase },
    }),
  },
  {
    path: 'positions/new',
    title: 'New job position - HireLab',
    loadComponent: () => import('../positions/position-form-page').then((m) => m.PositionFormPage),
    data: staffPage({ roles: ['Manager'] }),
  },
  {
    path: 'positions/:positionId',
    title: 'Job position - HireLab',
    loadComponent: () =>
      import('../positions/position-detail-page').then((m) => m.PositionDetailPage),
    data: staffPage({ roles: ['Manager', 'Interviewer'] }),
  },
  {
    path: 'positions/:positionId/edit',
    title: 'Edit job position - HireLab',
    loadComponent: () => import('../positions/position-form-page').then((m) => m.PositionFormPage),
    data: staffPage({ roles: ['Manager'] }),
  },
  {
    path: 'positions/:positionId/candidates/new',
    title: 'Add candidate - HireLab',
    loadComponent: () =>
      import('../candidates/candidate-form-page').then((m) => m.CandidateFormPage),
    data: staffPage({ roles: ['Manager', 'Interviewer'] }),
  },
  {
    path: 'candidates/:candidateId',
    title: 'Candidate - HireLab',
    loadComponent: () =>
      import('../candidates/candidate-profile-page').then((m) => m.CandidateProfilePage),
    data: staffPage({ roles: ['Manager', 'Interviewer'] }),
  },
  {
    path: 'candidates/:candidateId/edit',
    title: 'Edit candidate - HireLab',
    loadComponent: () =>
      import('../candidates/candidate-form-page').then((m) => m.CandidateFormPage),
    data: staffPage({ roles: ['Manager', 'Interviewer'] }),
  },
  {
    path: 'tasks',
    title: 'Task bank - HireLab',
    loadComponent: placeholder,
    data: staffPage({
      roles: ['Manager', 'Interviewer'],
      nav: { label: 'Task bank', icon: lucideLibraryBig },
    }),
  },
  {
    path: 'team',
    title: 'My team - HireLab',
    loadComponent: () => import('../pages/my-team-page').then((m) => m.MyTeamPage),
    data: staffPage({ roles: ['Manager'], nav: { label: 'My team', icon: lucideUsersRound } }),
  },
  {
    path: 'teams',
    title: 'Teams - HireLab',
    loadComponent: () => import('../pages/teams-page').then((m) => m.TeamsPage),
    data: staffPage({ roles: ['Admin'], nav: { label: 'Teams', icon: lucideUsersRound } }),
  },
  {
    path: 'users',
    title: 'Users - HireLab',
    loadComponent: () => import('../pages/users-page').then((m) => m.UsersPage),
    data: staffPage({ roles: ['Admin'], nav: { label: 'Users', icon: lucideUsers } }),
  },
  {
    path: 'account/password',
    title: 'Change password - HireLab',
    loadComponent: () =>
      import('../account/change-password-page').then((m) => m.ChangePasswordPage),
    data: staffPage({ roles: ALL_ROLES }),
  },
];
