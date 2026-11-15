import { Routes } from '@angular/router';

export const designRoutes: Routes = [
  {
    path: 'design',
    title: 'Design system - HireLab',
    loadComponent: () => import('./design-page').then((m) => m.DesignPage),
  },
];
